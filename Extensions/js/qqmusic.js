var Q_SERVER_URL = "http://qqmusic.qq.com/fcgi-bin/qm_getLyricId.fcg?";
var R_SERVER_URL = "http://music.qq.com/miniportal/static/lyric/";
var QQFilter = " _-*.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

function QQStringFilter(s) {
    s = s.toLowerCase();
    s = s.replace(/\'|·|\&|–/g, "");
    //trim all spaces
    s = s.replace(/(\s*)|(\s*$)/g, "");
    //truncate all symbols
    s = s.replace(/\(.*?\)|\[.*?]|{.*?}|（.*?/g, "");
    s = s.replace(/[-/:-@[-`{-~]+/g, "");
    s = s.replace(/[\u2014\u2018\u201c\u2026\u3001\u3002\u300a\u300b\u300e\u300f\u3010\u3011\u30fb\uff01\uff08\uff09\uff0c\uff1a\uff1b\uff1f\uff5e\uffe5]+/g, "");
    var ret = "";
    for (var i = 0; i < s.length; i++) {
        if (!(QQFilter.indexOf(s[i]))) {
            ret += "+";
        } else if (QQFilter.indexOf(s[i].toUpperCase()) < 0) {
            if (s.charCodeAt[i] < 128) {
                ret += "%" + s.charCodeAt[i].toString(16).toUpperCase();
            } else {
                var d = urlEncode(s[i]);
                ret += d;
            }
        } else {
            ret += s[i];
        }
    }
    return ret;
}

function lookupLrc(title, artist) {
    var url = Q_SERVER_URL + "name=" + QQStringFilter(title) + "&singer=" + QQStringFilter(artist) + "&from=qqplayer";
    try {
        xmlhttp.open('GET', url, false);
        xmlhttp.send();
    } catch (e) {
        api.log('exception: ' + e.toString());
        return;
    }

    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        try {
            var xml_text = api.decodeAsCodePage(xmlhttp, 54936);
            var doc = new Windows.Data.Xml.Dom.XmlDocument();
            doc.loadXml(xml_text);
            var songs = doc.getElementsByTagName("songinfo");
            var names = doc.getElementsByTagName("name");
            var singers = doc.getElementsByTagName("singername");

            for (var i = 0; i < songs.length; i++) {
                var lrc = api.createLrc();
                lrc.opaque = songs[i].attributes.getNamedItem("id").nodeValue;
                lrc.artist = decodeURI(singers[i].childNodes[0].nodeValue).replace(/[-/:-@[-`{-~/+]+/g, " ");
                lrc.title = decodeURI(names[i].childNodes[0].nodeValue).replace(/[-/:-@[-`{-~/+]+/g, " ");
            }
        }
        catch (e) {
            api.log('exception: ' + e.toString());
        }
    }
}

function downloadLrc(lrcinfo) {
    var url = R_SERVER_URL + lrcinfo.opaque.toString().substr(-2) + "/" + lrcinfo.opaque + ".xml";

    try {
        xmlhttp.open('GET', url, false);
        xmlhttp.send();
    } catch (e) {
        api.log('exception: ' + e.toString());
        return;
    }

    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        var xml_text = api.decodeAsCodePage(xmlhttp, 54936);
        var doc = new Windows.Data.Xml.Dom.XmlDocument();
        doc.loadXml(xml_text);

        var lr = doc.getElementsByTagName("lyric");
        if (lr.length == 0) return;

        return lr[0].childNodes[0].nodeValue;
    }
}

function urlEncode(str) {
    var ret = "";
    var strSpecial = "!\"#$%&’()*+,/:;<=>?[]^`{|}~%";
    var tt = "";
    for (var i = 0; i < str.length; i++) {
        var chr = str.charAt(i);
        var c = str2asc(chr);
        tt += chr + ":" + c + "n";
        if (parseInt("0x" + c) > 0x7f) {
            ret += "%" + c.slice(0, 2) + "%" + c.slice(-2);
        }
        else {
            if (chr == " ")
                ret += "+";
            else if (strSpecial.indexOf(chr) != -1)
                ret += "%" + c.toString(16);
            else
                ret += chr;
        }
    }
    return ret;
}

function str2asc(str) {
    var n = api.charToCodePage(str, 54936);
    var s = n.toString(16);
    return s.toUpperCase();
}