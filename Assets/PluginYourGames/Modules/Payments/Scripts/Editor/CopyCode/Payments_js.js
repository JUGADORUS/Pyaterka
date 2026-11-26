var paymentsData = NO_DATA;
let payments = null;
const pyaterkaPermanentProductId = "pyaterka_premium_car";
let paymentsInitialization = null;
let paymentsRetryTimer = null;
let paymentsRetryCount = 0;

function InitPayments() {
    if (paymentsInitialization !== null) return paymentsInitialization;
    paymentsInitialization = LoadPayments().finally(() => { paymentsInitialization = null; });
    return paymentsInitialization;
}

async function LoadPayments() {
    try {
        if (ysdk == null) return Finish(NO_DATA);
        payments = await ysdk.getPayments();
        const [catalogResult, ownedResult] = await Promise.allSettled([
            payments.getCatalog(), payments.getPurchases()
        ]);
        if (catalogResult.status !== "fulfilled")
            console.error("[Pyaterka Payments] getCatalog failed:", PaymentError(catalogResult.reason));
        if (ownedResult.status !== "fulfilled")
            console.error("[Pyaterka Payments] getPurchases failed; VIP purchase blocked until ownership is verified:", PaymentError(ownedResult.reason));
        const ownedProductIds = ownedResult.status === "fulfilled"
            ? ownedResult.value.map(purchase => purchase.productID)
            : [];
        let products = catalogResult.status === "fulfilled" ? catalogResult.value : [];
        const hasPremium = products.some(product => product.id === pyaterkaPermanentProductId);
        if (catalogResult.status === "fulfilled" && !hasPremium)
            console.warn("[Pyaterka Payments] VIP product missing from SDK catalog. Expected:",
                pyaterkaPermanentProductId, "Received IDs:", products.map(product => product.id));
        if (catalogResult.status !== "fulfilled" || ownedResult.status !== "fulfilled" || !hasPremium)
            RetryPayments();
        else if (paymentsRetryTimer !== null) {
            clearTimeout(paymentsRetryTimer);
            paymentsRetryTimer = null;
        }
        if (ownedResult.status !== "fulfilled") {
            products = products.filter(product => product.id !== pyaterkaPermanentProductId);
        }
        return Finish(JSON.stringify({
            id: products.map(product => product.id),
            title: products.map(product => product.title),
            description: products.map(product => product.description),
            imageURI: products.map(product => product.imageURI),
            price: products.map(product => product.price),
            priceValue: products.map(product => product.priceValue),
            priceCurrencyCode: products.map(product => product.priceCurrencyCode),
            currencyImageURL: products.map(PaymentCurrencyImage),
            consumed: products.map(product => !ownedProductIds.includes(product.id)),
            ownedProductIds,
            language: ysdk.environment.i18n.lang
        }));
    } catch (error) {
        console.error("[Pyaterka Payments] Could not initialize purchases:", PaymentError(error));
        RetryPayments();
        return Finish(NO_DATA);
    }

    function Finish(result) {
        paymentsData = result;
        YG2Instance("PaymentsEntries", result);
        return result;
    }
}

function PaymentError(error) {
    return error ? [error.code, error.message || String(error)].filter(Boolean).join(": ") : "Unknown error";
}

function PaymentCurrencyImage(product) {
    // An optional display icon must not make the entire catalog unavailable.
    try {
        return typeof product.getPriceCurrencyImage === "function" ? product.getPriceCurrencyImage("medium") : "";
    } catch (error) {
        console.warn("[Pyaterka Payments] Currency icon unavailable:", PaymentError(error));
        return "";
    }
}

function RetryPayments() {
    if (paymentsRetryTimer !== null || paymentsRetryCount >= 2) return;
    const delay = [2000, 5000][paymentsRetryCount++];
    paymentsRetryTimer = setTimeout(() => {
        paymentsRetryTimer = null;
        InitPayments();
    }, delay);
}

async function BuyPayments(id) {
    try {
        if (payments == null) throw new Error("Payments are unavailable");
        if (id === pyaterkaPermanentProductId) {
            // Recheck before opening checkout: a delayed restoration must not charge twice.
            const owned = await payments.getPurchases();
            if (owned.some(purchase => purchase.productID === id)) {
                YG2Instance("OnPurchaseSuccess", id);
                return;
            }
        }
        const purchase = await payments.purchase({ id });
        if (purchase.productID !== id) throw new Error("Unexpected purchased product");
        if (id === pyaterkaPermanentProductId) {
            YG2Instance("OnPurchaseSuccess", id);
        } else {
            await ConsumePurchase(id, true);
        }
    } catch (error) {
        if (id === pyaterkaPermanentProductId && payments != null) {
            try {
                const purchases = await payments.getPurchases();
                if (purchases.some(purchase => purchase.productID === id)) {
                    YG2Instance("OnPurchaseSuccess", id);
                    return;
                }
            } catch (refreshError) {
                console.error("Could not verify purchase ownership", refreshError);
            }
        }
        console.error("Purchase did not complete", error);
        YG2Instance("OnPurchaseFailed", id);
    } finally {
        FocusGame();
    }
}

async function ConsumePurchase(id, onPurchaseSuccess) {
    if (id === pyaterkaPermanentProductId || payments == null) return;
    const purchases = await payments.getPurchases();
    for (const purchase of purchases) {
        if (purchase.productID !== id) continue;
        await payments.consumePurchase(purchase.purchaseToken);
        if (onPurchaseSuccess) YG2Instance("OnPurchaseSuccess", id);
    }
}

async function ConsumePurchases(onPurchaseSuccess) {
    if (payments == null) return;
    try {
        const purchases = await payments.getPurchases();
        for (const purchase of purchases) {
            if (purchase.productID === pyaterkaPermanentProductId) continue;
            await payments.consumePurchase(purchase.purchaseToken);
            if (onPurchaseSuccess) YG2Instance("OnPurchaseSuccess", purchase.productID);
        }
    } catch (error) {
        console.error("Could not consume purchases", error);
    }
}
