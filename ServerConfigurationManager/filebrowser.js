function initApplication(instance) {
var internalContainer = function() {
var applicationInstance = instance;
applicationInstance.set_title('File browser - Root');
var content = applicationInstance.GetApplicationContent().children[0];




UI.GetProperties(UI.GetElementById(content,'filelist')).getContextMenu = function(e) {
var retval = new Array();
var doalert = function() {
if(e.parentElement.getAttribute('isdirectory') == 'True') {
//Request directory
network.GetResource('getDirectory.htm?sessionID='+sessionID+'&dirpath='+escape(e.parentElement.getAttribute('fullname')),function(response) {
UI.GetElementById(content,'filelist').innerHTML = response;
applicationInstance.set_title('File browser - '+e.parentElement.getAttribute('fullname'));
});
}else {
var url = 'downloadManager.htm?sessionID='+sessionID+'&downloadFile='+escape(e.parentElement.getAttribute('fullname'));
var tempframe = document.createElement('iframe');
document.body.appendChild(tempframe);
tempframe.style.display = 'none';
tempframe.src = url;
}
}
if(e.parentElement.getAttribute('isdirectory') == 'True') {
retval.push({label:'Open directory',onclick:doalert},{label:'Exit menu',onclick:null});
}else {
retval.push({label:'Download',onclick:doalert},{label:'Exit menu',onclick:null});

}
return retval;
}
	}
	internalContainer();
	}