/* big.js v3.1.3 https://github.com/MikeMcl/big.js/LICENCE */(function (global) { "use strict"; var DP = 20, RM = 1, MAX_DP = 1e6, MAX_POWER = 1e6, E_NEG = -7, E_POS = 21, P = {}, isValid = /^-?(\d+(\.\d*)?|\.\d+)(e[+-]?\d+)?$/i, Big; function bigFactory() { function Big(n) { var x = this; if (!(x instanceof Big)) { return n === void 0 ? bigFactory() : new Big(n) } if (n instanceof Big) { x.s = n.s; x.e = n.e; x.c = n.c.slice() } else { parse(x, n) } x.constructor = Big } Big.prototype = P; Big.DP = DP; Big.RM = RM; Big.E_NEG = E_NEG; Big.E_POS = E_POS; return Big } function format(x, dp, toE) { var Big = x.constructor, i = dp - (x = new Big(x)).e, c = x.c; if (c.length > ++dp) { rnd(x, i, Big.RM) } if (!c[0]) { ++i } else if (toE) { i = dp } else { c = x.c; i = x.e + i + 1 } for (; c.length < i; c.push(0)) { } i = x.e; return toE === 1 || toE && (dp <= i || i <= Big.E_NEG) ? (x.s < 0 && c[0] ? "-" : "") + (c.length > 1 ? c[0] + "." + c.join("").slice(1) : c[0]) + (i < 0 ? "e" : "e+") + i : x.toString() } function parse(x, n) { var e, i, nL; if (n === 0 && 1 / n < 0) { n = "-0" } else if (!isValid.test(n += "")) { throwErr(NaN) } x.s = n.charAt(0) == "-" ? (n = n.slice(1), -1) : 1; if ((e = n.indexOf(".")) > -1) { n = n.replace(".", "") } if ((i = n.search(/e/i)) > 0) { if (e < 0) { e = i } e += +n.slice(i + 1); n = n.substring(0, i) } else if (e < 0) { e = n.length } for (i = 0; n.charAt(i) == "0"; i++) { } if (i == (nL = n.length)) { x.c = [x.e = 0] } else { for (; n.charAt(--nL) == "0";) { } x.e = e - i - 1; x.c = []; for (e = 0; i <= nL; x.c[e++] = +n.charAt(i++)) { } } return x } function rnd(x, dp, rm, more) { var u, xc = x.c, i = x.e + dp + 1; if (rm === 1) { more = xc[i] >= 5 } else if (rm === 2) { more = xc[i] > 5 || xc[i] == 5 && (more || i < 0 || xc[i + 1] !== u || xc[i - 1] & 1) } else if (rm === 3) { more = more || xc[i] !== u || i < 0 } else { more = false; if (rm !== 0) { throwErr("!Big.RM!") } } if (i < 1 || !xc[0]) { if (more) { x.e = -dp; x.c = [1] } else { x.c = [x.e = 0] } } else { xc.length = i--; if (more) { for (; ++xc[i] > 9;) { xc[i] = 0; if (!i--) { ++x.e; xc.unshift(1) } } } for (i = xc.length; !xc[--i]; xc.pop()) { } } return x } function throwErr(message) { var err = new Error(message); err.name = "BigError"; throw err } P.abs = function () { var x = new this.constructor(this); x.s = 1; return x }; P.cmp = function (y) { var xNeg, x = this, xc = x.c, yc = (y = new x.constructor(y)).c, i = x.s, j = y.s, k = x.e, l = y.e; if (!xc[0] || !yc[0]) { return !xc[0] ? !yc[0] ? 0 : -j : i } if (i != j) { return i } xNeg = i < 0; if (k != l) { return k > l ^ xNeg ? 1 : -1 } i = -1; j = (k = xc.length) < (l = yc.length) ? k : l; for (; ++i < j;) { if (xc[i] != yc[i]) { return xc[i] > yc[i] ^ xNeg ? 1 : -1 } } return k == l ? 0 : k > l ^ xNeg ? 1 : -1 }; P.div = function (y) { var x = this, Big = x.constructor, dvd = x.c, dvs = (y = new Big(y)).c, s = x.s == y.s ? 1 : -1, dp = Big.DP; if (dp !== ~~dp || dp < 0 || dp > MAX_DP) { throwErr("!Big.DP!") } if (!dvd[0] || !dvs[0]) { if (dvd[0] == dvs[0]) { throwErr(NaN) } if (!dvs[0]) { throwErr(s / 0) } return new Big(s * 0) } var dvsL, dvsT, next, cmp, remI, u, dvsZ = dvs.slice(), dvdI = dvsL = dvs.length, dvdL = dvd.length, rem = dvd.slice(0, dvsL), remL = rem.length, q = y, qc = q.c = [], qi = 0, digits = dp + (q.e = x.e - y.e) + 1; q.s = s; s = digits < 0 ? 0 : digits; dvsZ.unshift(0); for (; remL++ < dvsL; rem.push(0)) { } do { for (next = 0; next < 10; next++) { if (dvsL != (remL = rem.length)) { cmp = dvsL > remL ? 1 : -1 } else { for (remI = -1, cmp = 0; ++remI < dvsL;) { if (dvs[remI] != rem[remI]) { cmp = dvs[remI] > rem[remI] ? 1 : -1; break } } } if (cmp < 0) { for (dvsT = remL == dvsL ? dvs : dvsZ; remL;) { if (rem[--remL] < dvsT[remL]) { remI = remL; for (; remI && !rem[--remI]; rem[remI] = 9) { } --rem[remI]; rem[remL] += 10 } rem[remL] -= dvsT[remL] } for (; !rem[0]; rem.shift()) { } } else { break } } qc[qi++] = cmp ? next : ++next; if (rem[0] && cmp) { rem[remL] = dvd[dvdI] || 0 } else { rem = [dvd[dvdI]] } } while ((dvdI++ < dvdL || rem[0] !== u) && s--); if (!qc[0] && qi != 1) { qc.shift(); q.e-- } if (qi > digits) { rnd(q, dp, Big.RM, rem[0] !== u) } return q }; P.eq = function (y) { return !this.cmp(y) }; P.gt = function (y) { return this.cmp(y) > 0 }; P.gte = function (y) { return this.cmp(y) > -1 }; P.lt = function (y) { return this.cmp(y) < 0 }; P.lte = function (y) { return this.cmp(y) < 1 }; P.sub = P.minus = function (y) { var i, j, t, xLTy, x = this, Big = x.constructor, a = x.s, b = (y = new Big(y)).s; if (a != b) { y.s = -b; return x.plus(y) } var xc = x.c.slice(), xe = x.e, yc = y.c, ye = y.e; if (!xc[0] || !yc[0]) { return yc[0] ? (y.s = -b, y) : new Big(xc[0] ? x : 0) } if (a = xe - ye) { if (xLTy = a < 0) { a = -a; t = xc } else { ye = xe; t = yc } t.reverse(); for (b = a; b--; t.push(0)) { } t.reverse() } else { j = ((xLTy = xc.length < yc.length) ? xc : yc).length; for (a = b = 0; b < j; b++) { if (xc[b] != yc[b]) { xLTy = xc[b] < yc[b]; break } } } if (xLTy) { t = xc; xc = yc; yc = t; y.s = -y.s } if ((b = (j = yc.length) - (i = xc.length)) > 0) { for (; b--; xc[i++] = 0) { } } for (b = i; j > a;) { if (xc[--j] < yc[j]) { for (i = j; i && !xc[--i]; xc[i] = 9) { } --xc[i]; xc[j] += 10 } xc[j] -= yc[j] } for (; xc[--b] === 0; xc.pop()) { } for (; xc[0] === 0;) { xc.shift(); --ye } if (!xc[0]) { y.s = 1; xc = [ye = 0] } y.c = xc; y.e = ye; return y }; P.mod = function (y) { var yGTx, x = this, Big = x.constructor, a = x.s, b = (y = new Big(y)).s; if (!y.c[0]) { throwErr(NaN) } x.s = y.s = 1; yGTx = y.cmp(x) == 1; x.s = a; y.s = b; if (yGTx) { return new Big(x) } a = Big.DP; b = Big.RM; Big.DP = Big.RM = 0; x = x.div(y); Big.DP = a; Big.RM = b; return this.minus(x.times(y)) }; P.add = P.plus = function (y) { var t, x = this, Big = x.constructor, a = x.s, b = (y = new Big(y)).s; if (a != b) { y.s = -b; return x.minus(y) } var xe = x.e, xc = x.c, ye = y.e, yc = y.c; if (!xc[0] || !yc[0]) { return yc[0] ? y : new Big(xc[0] ? x : a * 0) } xc = xc.slice(); if (a = xe - ye) { if (a > 0) { ye = xe; t = yc } else { a = -a; t = xc } t.reverse(); for (; a--; t.push(0)) { } t.reverse() } if (xc.length - yc.length < 0) { t = yc; yc = xc; xc = t } a = yc.length; for (b = 0; a;) { b = (xc[--a] = xc[a] + yc[a] + b) / 10 | 0; xc[a] %= 10 } if (b) { xc.unshift(b); ++ye } for (a = xc.length; xc[--a] === 0; xc.pop()) { } y.c = xc; y.e = ye; return y }; P.pow = function (n) { var x = this, one = new x.constructor(1), y = one, isNeg = n < 0; if (n !== ~~n || n < -MAX_POWER || n > MAX_POWER) { throwErr("!pow!") } n = isNeg ? -n : n; for (; ;) { if (n & 1) { y = y.times(x) } n >>= 1; if (!n) { break } x = x.times(x) } return isNeg ? one.div(y) : y }; P.round = function (dp, rm) { var x = this, Big = x.constructor; if (dp == null) { dp = 0 } else if (dp !== ~~dp || dp < 0 || dp > MAX_DP) { throwErr("!round!") } rnd(x = new Big(x), dp, rm == null ? Big.RM : rm); return x }; P.sqrt = function () { var estimate, r, approx, x = this, Big = x.constructor, xc = x.c, i = x.s, e = x.e, half = new Big("0.5"); if (!xc[0]) { return new Big(x) } if (i < 0) { throwErr(NaN) } i = Math.sqrt(x.toString()); if (i === 0 || i === 1 / 0) { estimate = xc.join(""); if (!(estimate.length + e & 1)) { estimate += "0" } r = new Big(Math.sqrt(estimate).toString()); r.e = ((e + 1) / 2 | 0) - (e < 0 || e & 1) } else { r = new Big(i.toString()) } i = r.e + (Big.DP += 4); do { approx = r; r = half.times(approx.plus(x.div(approx))) } while (approx.c.slice(0, i).join("") !== r.c.slice(0, i).join("")); rnd(r, Big.DP -= 4, Big.RM); return r }; P.mul = P.times = function (y) { var c, x = this, Big = x.constructor, xc = x.c, yc = (y = new Big(y)).c, a = xc.length, b = yc.length, i = x.e, j = y.e; y.s = x.s == y.s ? 1 : -1; if (!xc[0] || !yc[0]) { return new Big(y.s * 0) } y.e = i + j; if (a < b) { c = xc; xc = yc; yc = c; j = a; a = b; b = j } for (c = new Array(j = a + b) ; j--; c[j] = 0) { } for (i = b; i--;) { b = 0; for (j = a + i; j > i;) { b = c[j] + yc[i] * xc[j - i - 1] + b; c[j--] = b % 10; b = b / 10 | 0 } c[j] = (c[j] + b) % 10 } if (b) { ++y.e } if (!c[0]) { c.shift() } for (i = c.length; !c[--i]; c.pop()) { } y.c = c; return y }; P.toString = P.valueOf = P.toJSON = function () { var x = this, Big = x.constructor, e = x.e, str = x.c.join(""), strL = str.length; if (e <= Big.E_NEG || e >= Big.E_POS) { str = str.charAt(0) + (strL > 1 ? "." + str.slice(1) : "") + (e < 0 ? "e" : "e+") + e } else if (e < 0) { for (; ++e; str = "0" + str) { } str = "0." + str } else if (e > 0) { if (++e > strL) { for (e -= strL; e--; str += "0") { } } else if (e < strL) { str = str.slice(0, e) + "." + str.slice(e) } } else if (strL > 1) { str = str.charAt(0) + "." + str.slice(1) } return x.s < 0 && x.c[0] ? "-" + str : str }; P.toExponential = function (dp) { if (dp == null) { dp = this.c.length - 1 } else if (dp !== ~~dp || dp < 0 || dp > MAX_DP) { throwErr("!toExp!") } return format(this, dp, 1) }; P.toFixed = function (dp) { var str, x = this, Big = x.constructor, neg = Big.E_NEG, pos = Big.E_POS; Big.E_NEG = -(Big.E_POS = 1 / 0); if (dp == null) { str = x.toString() } else if (dp === ~~dp && dp >= 0 && dp <= MAX_DP) { str = format(x, x.e + dp); if (x.s < 0 && x.c[0] && str.indexOf("-") < 0) { str = "-" + str } } Big.E_NEG = neg; Big.E_POS = pos; if (!str) { throwErr("!toFix!") } return str }; P.toPrecision = function (sd) { if (sd == null) { return this.toString() } else if (sd !== ~~sd || sd < 1 || sd > MAX_DP) { throwErr("!toPre!") } return format(this, sd - 1, 2) }; Big = bigFactory(); if (typeof define === "function" && define.amd) { define(function () { return Big }) } else if (typeof module !== "undefined" && module.exports) { module.exports = Big } else { global.Big = Big } })(this);

function calc_code(artist, title, id) {
    var info = artist + title;
    var utf8hex = GetUTF8HexString(info);
    var code = [];
    var len = utf8hex.length / 2;
    for (var i = 0; i < utf8hex.length; i += 2) {
        code[i / 2] = parseInt(utf8hex.substr(i, 2), 16);
    }
    var t1 = 0,
		t2 = 0,
		t3 = 0;
    t1 = (id & 0x0000FF00) >> 8;
    if ((id & 0x00FF0000) == 0) t3 = 0x000000FF & ~t1;
    else t3 = 0x000000FF & ((id & 0x00FF0000) >> 16);
    t3 = t3 | ((0x000000FF & id) << 8);
    t3 = t3 << 8;
    t3 = t3 | (0x000000FF & t1);
    t3 = t3 << 8;
    if ((id & 0xFF000000) == 0) t3 = t3 | (0x000000FF & (~id));
    else t3 = t3 | (0x000000FF & (id >> 24));

    var j = len - 1;
    while (j >= 0) {
        var c = code[j];
        if (c >= 0x80) c = c - 0x100;
        t1 = ((c + t2) & 0xFFFFFFFF);
        t2 = ((t2 << (j % 2 + 4)) & 0xFFFFFFFF);
        t2 = ((t1 + t2) & 0xFFFFFFFF);
        j--;
    }
    j = 0;
    t1 = 0;
    while (j < len) {
        var c = code[j];
        if (c >= 128) c = c - 256;
        var t4 = ((c + t1) & 0xFFFFFFFF);
        t1 = ((t1 << (j % 2 + 3)) & 0xFFFFFFFF);
        t1 = ((t1 + t4) & 0xFFFFFFFF);
        j++;
    }
    var t5 = conv(t2 ^ t3);
    t5 = conv(t5 + (t1 | id));
    t5 = conv(api.intMul(t5, (t1 | t3)));
    t5 = conv(api.intMul(t5, (t2 ^ id)));
    if (t5 > 0x80000000) t5 = (t5 - 0x100000000) & 0xFFFFFFFF;
    return t5;
}

//===========================TTPLAYER HELPER================================
//==========================================================================

//Unicode | UTF-8
//(HEX)   | (BIN)
//--------------------+---------------------------------------------
//0000 0000-0000 007F | 0xxxxxxx
//0000 0080-0000 07FF | 110xxxxx 10xxxxxx
//0000 0800-0000 FFFF | 1110xxxx 10xxxxxx 10xxxxxx
//0001 0000-0010 FFFF | 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
function GetUTF8HexString(str) {
    var ret = "";

    for (var i = 0; i < str.length; i++) {
        var c = str.charCodeAt(i);
        var b = 0;
        if (c < 0x0080) { // 0000 - 007F
            b = c & 0x000000ff;
        } else if (c < 0x800) { // 0080 - 07FF
            b = (0xC0 | ((c & 0x7C0) >> 6)) << 8;
            b |= (0x80 | (c & 0x3F)) << 0;
        } else if (c < 0x010000) { // 0800 - FFFF
            b = (0xE0 | ((c & 0xF000) >> 12)) << 16;
            b |= (0x80 | ((c & 0xFC0) >> 6)) << 8;
            b |= (0x80 | (c & 0x3F)) << 0;
        } else { // 0x010000 - 
            b = (0xF0 | ((c & 0x1C0000) >> 18)) << 24;
            b |= (0x80 | ((c & 0x3F000) >> 12)) << 16;
            b |= (0x80 | ((c & 0xFC0) >> 6)) << 8;
            b |= (0x80 | (c & 0x3F)) << 0;
        }
        ret += b.toString(16).toUpperCase();
    }

    return ret;
}

function GetUnicodeLEHexString(str) {
    var ret = "";
    for (var i = 0; i < str.length; i++) {
        var b = str.charCodeAt(i);
        var bs = "";

        var lb = (b & 0xff00) >> 8;
        var hb = (b & 0x00ff) >> 0;
        if (hb < 0x10) bs += "0";
        bs += hb.toString(16).toUpperCase();
        if (lb < 0x10) bs += "0";
        bs += lb.toString(16).toUpperCase();

        ret += bs;
    }
    return ret;
}

function conv(i) {
    i &= 0xFFFFFFFF;
    var r = i % 0x100000000;
    if (i >= 0 && r > 0x80000000) r = r - 0x100000000;
    if (i < 0 && r < 0x80000000) r = r + 0x100000000;
    return r & 0xFFFFFFFF;
}

function ProcessKeyword(str) {
    var s = str;
    s = s.toLowerCase();
    s = s.replace(/\'|·|\$|\&|–/g, "");
    s = s.replace(/(\s*)|(\s*$)/g, "");
    s = s.replace(/\(.*?\)|\[.*?]|{.*?}|（.*?/g, "");
    s = s.replace(/[-/:-@[-`{-~]+/g, "");
    s = s.replace(/[\u2014\u2018\u201c\u2026\u3001\u3002\u300a\u300b\u300e\u300f\u3010\u3011\u30fb\uff01\uff08\uff09\uff0c\uff1a\uff1b\uff1f\uff5e\uffe5]+/g, "");
    return s;
}

SERVER = "http://ttlrcct.qianqian.com";

function generate_url(artist, title, query, id) {
    var url = "";
    if (query) {
        title = ProcessKeyword(title);
        artist = ProcessKeyword(artist);
        var title_hexstr = GetUnicodeLEHexString(title);
        var artist_hexstr = GetUnicodeLEHexString(artist);
        url = SERVER + "/dll/lyricsvr.dll?sh?Artist=" + artist_hexstr + "&Title=" + title_hexstr + "&Flags=0";
    } else {
        var code = calc_code(artist, title, id);
        url = SERVER + "/dll/lyricsvr.dll?dl?Id=" + id + "&Code=" + code.toString(10);
    }
    return url;
}

function lookupLrc(title, artist) {
    try {
        xmlhttp.open("GET", generate_url(artist, title, true, 0), false);
        xmlhttp.send();
    } catch (e) {
        return;
    }

    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        var xml_text = xmlhttp.responseText;
        var doc = new Windows.Data.Xml.Dom.XmlDocument();
        doc.loadXml(xml_text);
        var lyrics = doc.getElementsByTagName("lrc");
        for (var i = 0; i < lyrics.length; i++) {
            var attributes = [];
            for (var j=0;j<lyrics[i].attributes.length;j++){
                attributes[lyrics[i].attributes[j].nodeName] = lyrics[i].attributes[j].nodeValue;
            }
            var lrc = api.createLrc();
            lrc.title = attributes["title"];
            lrc.artist = attributes["artist"];
            lrc.album = attributes["album"];
            lrc.opaque = attributes["id"];
        }
    }
}

function downloadLrc(info) {
    var url = generate_url(info.artist, info.title, false, info.opaque);
    try{
        xmlhttp.open("GET", url, false);
        xmlhttp.send();
    } catch (e) {
        return;
    }
    if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
        return xmlhttp.responseText;
    }
}