function initApplication(instance) {
var content = instance.GetApplicationContent();
var netresp = function(response) {
content.innerHTML = response;
var table = UI.GetElementById(content,'maintable');
UI.GetProperties(table).getContextMenu = function(srcElem) {
var retval = new Array();
retval.push({label:'Terminate application',onclick:function() {
if(confirm('Terminating an application will discard any unsaved data in that application, and disconnect any active users from it. Are you sure you want to continue?')) {
var appname = UI.GetElementById(srcElem.parentElement,'appname').innerText;
network.GetResource('applicationManager.htm?sessionID='+sessionID+'&action=terminate&id='+escape(appname),netresp);
}
}},{label:'Set as startup application',onclick:function() {
var appname = UI.GetElementById(srcElem.parentElement,'appname').innerText;
network.GetResource('applicationManager.htm?sessionID='+sessionID+'&action=startup&id='+escape(appname),netresp);
}
},{label:'Close menu',onclick:null});
return retval;
}
}
network.GetResource('applicationManager.htm?sessionID='+sessionID,netresp);

}