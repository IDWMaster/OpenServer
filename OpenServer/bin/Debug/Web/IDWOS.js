IDWOS = {
WindowSystem : {
CreateWindow: function(title, innerHTML,windowStation) {
var window = document.createElement('div');
var title = document.createElement('div');
title.innerHTML = title;
title.style.backgroundColor = 'gray';
window.appendChild(title);
var content = document.createElement('div');
content.style.backgroundColor = 'blue';
window.appendChild(content);
window.style.position = 'fixed';
window.style.left = '50px';
window.style.top = '100px';
window.style.width = '200px';
window.style.height = '100px';
windowStation.appendChild(window);
}
}
}
