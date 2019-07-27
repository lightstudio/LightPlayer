//json2.js
if (typeof JSON !== 'object') { JSON = {}; }
(function () {
    'use strict'; function f(n) { return n < 10 ? '0' + n : n; }
    if (typeof Date.prototype.toJSON !== 'function') {
        Date.prototype.toJSON = function (key) {
            return isFinite(this.valueOf()) ? this.getUTCFullYear() + '-' +
            f(this.getUTCMonth() + 1) + '-' +
            f(this.getUTCDate()) + 'T' +
            f(this.getUTCHours()) + ':' +
            f(this.getUTCMinutes()) + ':' +
            f(this.getUTCSeconds()) + 'Z' : null;
        }; String.prototype.toJSON = Number.prototype.toJSON = Boolean.prototype.toJSON = function (key) { return this.valueOf(); };
    }
    var cx = /[\u0000\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g, escapable = /[\\\"\x00-\x1f\x7f-\x9f\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g, gap, indent, meta = { '\b': '\\b', '\t': '\\t', '\n': '\\n', '\f': '\\f', '\r': '\\r', '"': '\\"', '\\': '\\\\' }, rep; function quote(string) { escapable.lastIndex = 0; return escapable.test(string) ? '"' + string.replace(escapable, function (a) { var c = meta[a]; return typeof c === 'string' ? c : '\\u' + ('0000' + a.charCodeAt(0).toString(16)).slice(-4); }) + '"' : '"' + string + '"'; }
    function str(key, holder) {
        var i, k, v, length, mind = gap, partial, value = holder[key]; if (value && typeof value === 'object' && typeof value.toJSON === 'function') { value = value.toJSON(key); }
        if (typeof rep === 'function') { value = rep.call(holder, key, value); }
        switch (typeof value) {
            case 'string': return quote(value); case 'number': return isFinite(value) ? String(value) : 'null'; case 'boolean': case 'null': return String(value); case 'object': if (!value) { return 'null'; }
                gap += indent; partial = []; if (Object.prototype.toString.apply(value) === '[object Array]') {
                    length = value.length; for (i = 0; i < length; i += 1) { partial[i] = str(i, value) || 'null'; }
                    v = partial.length === 0 ? '[]' : gap ? '[\n' + gap + partial.join(',\n' + gap) + '\n' + mind + ']' : '[' + partial.join(',') + ']'; gap = mind; return v;
                }
                if (rep && typeof rep === 'object') { length = rep.length; for (i = 0; i < length; i += 1) { if (typeof rep[i] === 'string') { k = rep[i]; v = str(k, value); if (v) { partial.push(quote(k) + (gap ? ': ' : ':') + v); } } } } else { for (k in value) { if (Object.prototype.hasOwnProperty.call(value, k)) { v = str(k, value); if (v) { partial.push(quote(k) + (gap ? ': ' : ':') + v); } } } }
                v = partial.length === 0 ? '{}' : gap ? '{\n' + gap + partial.join(',\n' + gap) + '\n' + mind + '}' : '{' + partial.join(',') + '}'; gap = mind; return v;
        }
    }
    if (typeof JSON.stringify !== 'function') {
        JSON.stringify = function (value, replacer, space) {
            var i; gap = ''; indent = ''; if (typeof space === 'number') { for (i = 0; i < space; i += 1) { indent += ' '; } } else if (typeof space === 'string') { indent = space; }
            rep = replacer; if (replacer && typeof replacer !== 'function' && (typeof replacer !== 'object' || typeof replacer.length !== 'number')) { throw new Error('JSON.stringify'); }
            return str('', { '': value });
        };
    }
    if (typeof JSON.parse !== 'function') {
        JSON.parse = function (text, reviver) {
            var j; function walk(holder, key) {
                var k, v, value = holder[key]; if (value && typeof value === 'object') { for (k in value) { if (Object.prototype.hasOwnProperty.call(value, k)) { v = walk(value, k); if (v !== undefined) { value[k] = v; } else { delete value[k]; } } } }
                return reviver.call(holder, key, value);
            }
            text = String(text); cx.lastIndex = 0; if (cx.test(text)) {
                text = text.replace(cx, function (a) {
                    return '\\u' +
                    ('0000' + a.charCodeAt(0).toString(16)).slice(-4);
                });
            }
            if (/^[\],:{}\s]*$/.test(text.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g, '@').replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g, ']').replace(/(?:^|:|,)(?:\s*\[)+/g, ''))) { j = eval('(' + text + ')'); return typeof reviver === 'function' ? walk({ '': j }, '') : j; }
            throw new SyntaxError('JSON.parse');
        };
    }
}());

neteaseheaders = {
    Accept: '*/*',
    "Cookie": "appver=1.5.0.75771",
    'Referer': 'http://music.163.com',
}

function rawHttpRequest(method, action, query) {
    var k = Object.keys(neteaseheaders);
    var url = action;
    for (var i = 0; i < k.length; i++) {
        xmlhttp.setRequestHeader(k[i], neteaseheaders[k[i]]);
    }
    if (method == 'GET') {
        try {
            xmlhttp.open('GET', url, false);
            xmlhttp.send();
        } catch (e) {
            api.log('exception: ' + e.toString());
            return;
        }
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            return xmlhttp.responseText;
        }
    }

    else if (method == 'POST') {
        try {
            xmlhttp.open('POST', url, false);
            xmlhttp.send(query);
        } catch (e) {
            api.log('exception: ' + e.toString());
            return;
        }
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            return xmlhttp.responseText;
        }
    }

}
function isBlank(str) {
    return (!str || str.length == 0 || /^\s*$/.test(str));
}

function lookupLrc(title, artist) {
    if (isBlank(title))
        return null;
    var req = 's=' + encodeURIComponent(title) + '&limit=60&type=1&offset=0&total=true';
    var result = rawHttpRequest('POST', 'http://music.163.com/api/search/suggest/web', req);
    if (result != null) {
        var res = JSON.parse(result);
        var songs = res["result"]["songs"];
        for (var i = 0; i < songs.length; i++) {
            song = songs[i];

            var artist = "";
            if (song.artists.length != 0)
                artist = song.artists[0].name;

            var lrc = api.createLrc();
            lrc.title = song.name;
            lrc.artist = artist;
            lrc.album = song.album.name;
            lrc.opaque = song.id;
        }
    }
}

function downloadLrc(info) {
    var id = info.opaque;
    var result = rawHttpRequest('GET', 'http://music.163.com/api/song/media?id=' + id, null);
    if (result != null) {
        var lrc = JSON.parse(result)["lyric"];
        return lrc;
    }
}