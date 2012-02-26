


//IT'S OVER 9000!
max_z_index = 9001;

WindowManager = {

CreateWindow:function(x, y, title, width, height, html) {
var newwindow = document.createElement('div');
newwindow.style.position = 'fixed';
newwindow.style.left = x+'px';
newwindow.style.top = y+'px';
var titlebar = document.createElement('div');
newwindow.appendChild(titlebar);
titlebar.innerHTML = title;
titlebar.style.backgroundColor = 'Grey';
newwindow.style.width = width+'px';
var xbtn = document.createElement('button');
xbtn.innerHTML = 'X';
xbtn.style.position = 'absolute';
xbtn.style.right = '0px';
titlebar.appendChild(xbtn);

var content = document.createElement('div');
content.style.backgroundImage = 'url(\'transparent.png\')';
content.style.width = width+'px';
content.style.height = height+'px';
content.innerHTML = html;
newwindow.appendChild(content);
document.body.appendChild(newwindow);
var ismousedown = false;
var prevpos;
titlebar.onmousedown = function(e) {
e.preventDefault();
newwindow.style.zIndex = max_z_index+50;
max_z_index+=50;
prevpos = e;
ismousedown = true;
document.body.onmousemove = function(e) {
if(ismousedown) {
if(prev) {

var x = e.clientX;
var y = e.clientY;
setElementX(newwindow,getElementX(newwindow)-(prevpos.clientX-x));
setElementY(newwindow,getElementY(newwindow)-(prevpos.clientY-y));
prevpos = e;
prev = false
}else {
prev = true;
}
}
}
}
titlebar.onmouseup = function(e) {
ismousedown = false;
document.body.onmousemove = null;
}
var prev = true;
var sysmen = document.getElementById('sysMenu');
var menuElement = document.createElement('span');
menuElement.innerHTML = title;
menuElement.style.borderStyle = 'solid';
menuElement.style.borderColor = 'Yellow';
var visible = true;
menuElement.onclick = function(e) {
if(visible) {
visible = false;
newwindow.style.display = 'none';
}else {
visible = true;
newwindow.style.display = '';
newwindow.style.zIndex = max_z_index+50;
max_z_index+=50;
}
}
sysmen.appendChild(menuElement);

var retval = {
Close: function() {
sysmen.removeChild(menuElement);
document.body.removeChild(newwindow);

},
set_title: function(atitle) {
title = atitle;
titlebar.removeChild(xbtn);
titlebar.innerHTML = atitle;
titlebar.appendChild(xbtn);
menuElement.innerHTML = atitle;
},
get_title:function() {
return title;
},
GetApplicationContent:function() {
return content.children[0];
},
onCloseRequested:function() {
return true;
}
}
xbtn.onclick = function(e) {
if(retval.onCloseRequested()) {
retval.Close();
}
}
return retval;

}
}