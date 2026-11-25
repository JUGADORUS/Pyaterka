const pyaterkaCloudUnavailable = "PYATERKA_CLOUD_UNAVAILABLE";
var cloudSaves = pyaterkaCloudUnavailable;

function SaveCloud(jsonData, flush) {
    if (player == null) {
        console.error('CRASH Save Cloud: ', 'Didnt have time to load');
        return;
    }
    try {
        player.setData({
            saves: [jsonData],
        }, flush).catch(error => console.error("Cloud save failed", error));
    } catch (e) {
        console.error('CRASH Save Cloud: ', e.message);
    }
}

async function LoadCloud() {
    let result = pyaterkaCloudUnavailable;
    if (ysdk != null && player != null) {
        try {
            const data = await player.getData(["saves"]);
            if (data.saves == null || (Array.isArray(data.saves) && data.saves.length === 0)) {
                result = NO_DATA;
            } else if (Array.isArray(data.saves) && typeof data.saves[0] === "string") {
                result = JSON.stringify(data.saves);
            }
        } catch (error) {
            console.error("Cloud load failed", error);
        }
    }

    cloudSaves = result;
    YG2Instance("SetLoadSaves", result);
    return result;
}
