// Функция меняет признак Visible на true у всех подъэлементов элемента div, начианя с индекса first числом num
function Test() { alert('Test'); }
function Extend(div, first, num) {
    //alert("Hi!");
    var elements = div.childNodes;
    var ind = first;
    var border = elements.length; if (first+num < border) border = first + num;
    for (ind=0; ind < elements.length; ind++) {
        if (ind >= first && ind < border) elements[ind].style.display = "block";
        else elements[ind].style.display = "none";
    }
    if (elements.length > 1) {
        var lastelement = elements[elements.length - 1];
        if (lastelement.className == "more") {
            if (elements.length - 1 < border || num == -1) {
                lastelement.style.display = "none";
            } else {
                lastelement.style.display = "block";
            }
        }
    }
}
function Extend2(div, first, num) {
    //alert("Hi!");
    var elements = div.childNodes;
    var ind = first;
    var border = elements.length; if (first + num < border) border = first + num;
    for (ind = 0; ind < elements.length; ind++) {
        var div = elements[ind];
        var img = div.children[0].children[0].children[0].children[0].children[0].children[0];
        //alert(nxt.nodeName);
        if (ind >= first && ind < border) {
            div.style.display = "block";
            img.src = img.getAttribute("ALT");
            //alert("" + img.getAttribute("ALT"));
        } else {
            div.style.display = "none";
        }
    }
}
function ChangeExtender(img, groupHtmlId) {
    if (img.className == "extenderclosed" && groupHtmlId.childNodes.length < 4) img.className = "extenderopenedpartially";
    if (img.className == "extenderclosed") {
        img.className = "extenderopenedpartially";
        img.src = "PublicuemCommon/icons/extend_part.gif";
        Extend(groupHtmlId, 0, 4);
    } else if (img.className == "extenderopenedpartially") {
        img.className = "extenderopened";
        img.src = "PublicuemCommon/icons/extend_open.gif";
        Extend(groupHtmlId, 0, 99);
    } else {
        img.className = "extenderclosed";
        img.src = "PublicuemCommon/icons/extend_closed.gif";
        Extend(groupHtmlId, 0, -1);
    }
}
function Press(element) {
    var parentDiv = element.parentNode;
    var elementList = parentDiv.childNodes;
    for (var i = 0; i < elementList.length; i++) {
        var el = elementList[i];
        if (el.nodeName != 'A') continue;
        if (el == element) el.className = "pressed";
        else el.className = "unpressed";
    }
}
function disappear()
{
    oSphere.style.visibility="hidden"; 
}
function reappear()
{
    oSphere.style.visibility="visible"; 
}
