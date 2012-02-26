__internalglobalProperties = {
}
network = {
    ///<summary>Sends a web request to the specified URL, and triggers the callback upon completion</summary>
    GetResource: function (URL, callback) {
        var XREQ = new XMLHttpRequest();
        XREQ.open('GET', URL, true);
        var onReqComplete = function() {
            if (XREQ.readyState == 4) {
            
                callback(XREQ.responseText);
                
            }
        }
        XREQ.onreadystatechange = onReqComplete;
        XREQ.send(null);
    }
};

UI = {
GetElementById: function(container,id) {
for(var i = 0;i<container.children.length;i++) {
var elem = container.children[i];
if(elem.getAttribute('inst_id') == id) {
return elem;
}
}
},
GetProperties:function(element) {
if(element.getAttribute('uniqueID') == null || element.getAttribute('uniqueID') == undefined) {
element.setAttribute('uniqueID',Math.random().toString());
}
if(__internalglobalProperties[element.getAttribute('uniqueID')] == undefined) {
__internalglobalProperties[element.getAttribute('uniqueID')] = {};
}
return __internalglobalProperties[element.getAttribute('uniqueID')];
}
}