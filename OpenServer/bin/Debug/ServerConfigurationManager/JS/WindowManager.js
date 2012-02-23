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
}
titlebar.onmouseup = function(e) {
ismousedown = false;
}
var prev = true;
titlebar.onmousemove = function(e) {
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
}