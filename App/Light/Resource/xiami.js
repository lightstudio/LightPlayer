
function process_keywords(str) {
    var s = str;
    s = s.toLowerCase();
    s = s.replace(/\'|·|\$|\&|–/g, '');
    //truncate all symbols
    s = s.replace(/\(.*?\)|\[.*?]|{.*?}|（.*?/g, '');
    s = s.replace(/[-/:-@[-`{-~]+/g, '');
    s = s.replace(/[\u2014\u2018\u201c\u2026\u3001\u3002\u300a\u300b\u300e\u300f\u3010\u3011\u30fb\uff01\uff08\uff09\uff0c\uff1a\uff1b\uff1f\uff5e\uffe5]+/g, '');
    return s;
}


function lookupLrc(title, artist) {
    title = process_keywords(title);
    artist = process_keywords(artist);
    var url = 'http://www.xiami.com/search/song-lyric?key=' + title + '+' + artist;
    try {
        xmlhttp.open('GET', url, false);
        xmlhttp.send();
    } catch (e) {
        api.log('exception: ' + e.toString());
        return;
    }

    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        var rex = new RegExp('<a.*?href=\".*?/song/(\\d+).*?><b.*?key_red', 'g');
        var songid = [];
        for (var i = 0; ; i++) {
            var ret = rex.exec(xmlhttp.responseText);
            if (ret == null) break;
            songid[i] = ret[1];
        }
        for (var i = 0; i < songid.length; i++) {
            url = 'http://www.xiami.com/song/playlist/id/' + songid[i];
            try {
                xmlhttp.open('GET', url, false);
                xmlhttp.send();
            } catch (e) {
                api.log('exception: ' + e.toString());
                continue;
            }
            if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
                try {
                    var xml_text = xmlhttp.responseText;
                    var doc = new Windows.Data.Xml.Dom.XmlDocument();
                    doc.loadXml(xml_text);
                    var track_list = doc.getElementsByTagName('trackList');
                    var count = 0;
                    for (var j = 0; j < track_list.length; j++) {
                        var track = track_list[j].getElementsByTagName('track');
                        for (var k = 0; k < track.length; k++, count++) {
                            var lrc = api.createLrc();
                            lrc.title = track[k].getElementsByTagName('title')[0].childNodes[0].nodeValue;
                            lrc.artist = track[k].getElementsByTagName('artist')[0].childNodes[0].nodeValue;
                            lrc.album = track[k].getElementsByTagName('album')[0].childNodes[0].nodeValue;
                            lrc.opaque = track[k].getElementsByTagName('lyric')[0].childNodes[0].nodeValue;
                        }
                    }
                }
                catch (e) {
                    api.log('exception: ' + e.toString());
                }
            }
        }
    }
}

function downloadLrc(lrcinfo) {
    if (typeof lrcinfo.opaque !="string") {
        return;
    }
    try {
        xmlhttp.open('GET', lrcinfo.opaque, false);
        xmlhttp.send();
    } catch (e) {
        api.log('exception: ' + e.toString());
        return;
    }
    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        return xmlhttp.responseText;
    }
}
