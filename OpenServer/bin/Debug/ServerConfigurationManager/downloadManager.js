function initApplication(instance) {
var content = instance.GetApplicationContent();
var downloadComplete = function(response) {
content.innerHTML = response;
network.GetResource('downloadManager.htm?sessionID='+sessionID,downloadComplete);

}
network.GetResource('downloadManager.htm?sessionID='+sessionID,downloadComplete);
}