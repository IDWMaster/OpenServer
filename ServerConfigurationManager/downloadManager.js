

function initApplication(instance) {
var content = instance.GetApplicationContent();
var downloadComplete = function(response) {
content.innerHTML = response;
network.GetResource('downloadManager.htm?sessionID='+sessionID,downloadComplete);
var mtable = UI.GetElementById(content,'maintable');
UI.GetProperties(mtable).getContextMenu = function(elem) {
//where elem is the element that was clicked by the user
//BEGIN CONTEXT MENU
var retval = new Array();
if(UI.GetElementById(elem.parentElement,'status').innerHTML.indexOf('PAUSED')>-1) {
retval.push({label:'Resume download',onclick:function() {
network.GetResource('downloadManager.htm?sessionID='+sessionID+'&action=resume&inst='+elem.parentElement.getAttribute('dlID'),null);
}
});

}else {
retval.push({label:'Pause download',onclick:function() {
network.GetResource('downloadManager.htm?sessionID='+sessionID+'&action=pause&inst='+elem.parentElement.getAttribute('dlID'),null);
}
});
}
retval.push({label:'Abort download',onclick:function() {
network.GetResource('downloadManager.htm?sessionID='+sessionID+'&action=abort&inst='+elem.parentElement.getAttribute('dlID'),null);
}
});
return retval;
//END CONTEXT MENU
}
}
network.GetResource('downloadManager.htm?sessionID='+sessionID,downloadComplete);
}