var paymentsData = NO_DATA;
let payments = null;
const pyaterkaPermanentProductId = "pyaterka_premium_car";

async function InitPayments() {
    try {
        if (ysdk == null) return Finish(NO_DATA);
        payments = await ysdk.getPayments();
        const [catalogResult, ownedResult] = await Promise.allSettled([
            payments.getCatalog(), payments.getPurchases()
        ]);
        const ownedProductIds = ownedResult.status === "fulfilled"
            ? ownedResult.value.map(purchase => purchase.productID)
            : [];
        let products = catalogResult.status === "fulfilled" ? catalogResult.value : [];
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
            currencyImageURL: products.map(product => product.getPriceCurrencyImage("medium")),
            consumed: products.map(product => !ownedProductIds.includes(product.id)),
            ownedProductIds,
            language: ysdk.environment.i18n.lang
        }));
    } catch (error) {
        console.error("Could not initialize purchases", error);
        return Finish(NO_DATA);
    }

    function Finish(result) {
        paymentsData = result;
        YG2Instance("PaymentsEntries", result);
        return result;
    }
}

async function BuyPayments(id) {
    try {
        if (payments == null) throw new Error("Payments are unavailable");
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
