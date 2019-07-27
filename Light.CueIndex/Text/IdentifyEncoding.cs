// from http://blog.csdn.net/yenange/article/details/7209973

using System;

namespace Light.Text
{

    #region Class IdentifyEncoding.....
    /// <summary>
    /// 检测字符编码的类
    /// <seealso cref="System.IO.Stream"/>
    /// <seealso cref="System.Uri"/>
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// <strong>IdentifyEncoding</strong> 用来检测 <see cref="Uri"/>,<see cref="System.IO.FileInfo"/>,<see cref="sbyte"/> 字节数组的编码．
    /// Create By lion  <br/>
    /// 2005-02-21 22:00  <br/>
    /// Support .Net Framework v1.1.4322 <br/> 
    /// WebSite：www.lionsky.net(lion-a AT sohu.com) <br/> 
    /// ]]>
    /// </remarks>
    sealed class IdentifyEncoding
    {
        #region Fields.....

        // Frequency tables to hold the GB, Big5, and EUC-TW character
        // frequencies
        internal static int[][] GbFreq = new int[94][];
        internal static int[][] GbkFreq = new int[126][];
        internal static int[][] Big5Freq = new int[94][];
        internal static int[][] EucTwFreq = new int[94][];

        internal static string[] CodepageName =
            { "gb2312", "gb2312"/*should be GBK*/, "gb2312", "big5", "gb2312", "gb2312", "utf-8", "Unicode", "gb2312", null/*"OTHER"*/ };

        #endregion

        #region Methods.....

        /// <summary>
        /// 初始化 <see cref="IdentifyEncoding"/> 的实例
        /// </summary>
        public IdentifyEncoding()
        {
            Initialize_Frequencies();
        }

        #region GetEncodingName.....

        public string GetEncodingName(System.IO.Stream chinesefile, bool close = false)
        {
            var rawtext = new sbyte[chinesefile.Length];
            ReadInput(chinesefile, ref rawtext, 0, rawtext.Length);

            if (close)
            {
                chinesefile.Dispose();
            }

            return GetEncodingName(rawtext);
        }


        /// <summary>
        /// 从指定的 <see cref="sbyte"/> 字节数组中判断编码类型
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="System.IO.FileInfo"/> </param>
        /// <returns>返回编码类型("GB2312", "GBK", "HZ", "Big5", "CNS 11643", "ISO 2022CN", "UTF-8", "Unicode", "ASCII", "OTHER")</returns>
        /// <example>
        /// 以下示例演示了如何调用 <see cref="GetEncodingName(sbyte[])"/> 方法：
        /// <code>
        ///  IdentifyEncoding ide = new IdentifyEncoding();
        ///  Response.Write(ide.GetEncodingName(IdentifyEncoding.ToSByteArray(System.Text.Encoding.GetEncoding("gb2312").GetBytes("Lion互动网络(www.lionsky.net)")))); 
        /// </code>
        /// </example>
        public string GetEncodingName(sbyte[] rawtext)
        {
            int index, maxscore = 0;
            int encodingGuess = 0;

            var scores = new int[10];
            //分析编码的概率
            scores[0] = Gb2312Probability(rawtext);
            scores[1] = GbkProbability(rawtext);
            scores[2] = HzProbability(rawtext);
            scores[3] = Big5Probability(rawtext);
            scores[4] = EnctwProbability(rawtext);
            scores[5] = Iso2022CnProbability(rawtext);
            scores[6] = Utf8Probability(rawtext);
            scores[7] = UnicodeProbability(rawtext);
            scores[8] = AsciiProbability(rawtext);
            scores[9] = 0;

            // Tabulate Scores
            for (index = 0; index < 10; index++)
            {
                if (scores[index] > maxscore)
                {
                    encodingGuess = index;
                    maxscore = scores[index];
                }
            }

            // Return OTHER if nothing scored above 50
            if (maxscore <= 50)
            {
                encodingGuess = 9;
            }

            return CodepageName[encodingGuess];
        }

        #endregion

        #region About Probability.....

        #region GB2312Probability

        /// <summary>
        /// 判断是GB2312编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int Gb2312Probability(sbyte[] rawtext)
        {
            int i, rawtextlen;

            int dbchars = 1, gbchars = 1;
            long gbfreq = 0, totalfreq = 1;
            float rangeval, freqval;

            // Stage 1:  Check to see if characters fit into acceptable ranges

            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen - 1; i++)
            {
                if (rawtext[i] >= 0)
                {
                    //asciichars++;
                }
                else
                {
                    dbchars++;
                    if ((sbyte)Identity(0xA1) <= rawtext[i] && rawtext[i] <= (sbyte)Identity(0xF7) && (sbyte)Identity(0xA1) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xFE))
                    {
                        gbchars++;
                        totalfreq += 500;
                        var row = rawtext[i] + 256 - 0xA1;
                        var column = rawtext[i + 1] + 256 - 0xA1;
                        if (GbFreq[row][column] != 0)
                        {
                            gbfreq += GbFreq[row][column];
                        }
                        else if (15 <= row && row < 55)
                        {
                            gbfreq += 200;
                        }
                    }
                    i++;
                }
            }

            rangeval = 50 * (gbchars / (float)dbchars);
            freqval = 50 * (gbfreq / (float)totalfreq);


            return (int)(rangeval + freqval);
        }

        #endregion

        #region GBKProbability.....

        /// <summary>
        /// 判断是GBK编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int GbkProbability(sbyte[] rawtext)
        {
            int i, rawtextlen;

            int dbchars = 1, gbchars = 1;
            long gbfreq = 0, totalfreq = 1;
            float rangeval, freqval;

            // Stage 1:  Check to see if characters fit into acceptable ranges
            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen - 1; i++)
            {
                if (rawtext[i] >= 0)
                {
                    //asciichars++;
                }
                else
                {
                    dbchars++;
                    int row;
                    int column;
                    if ((sbyte)Identity(0xA1) <= rawtext[i] && rawtext[i] <= (sbyte)Identity(0xF7) && (sbyte)Identity(0xA1) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xFE))
                    {
                        gbchars++;
                        totalfreq += 500;
                        row = rawtext[i] + 256 - 0xA1;
                        column = rawtext[i + 1] + 256 - 0xA1;

                        if (GbFreq[row][column] != 0)
                        {
                            gbfreq += GbFreq[row][column];
                        }
                        else if (15 <= row && row < 55)
                        {
                            gbfreq += 200;
                        }
                    }
                    else if ((sbyte)Identity(0x81) <= rawtext[i] && rawtext[i] <= (sbyte)Identity(0xFE) && (((sbyte)Identity(0x80) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xFE)) || (0x40 <= rawtext[i + 1] && rawtext[i + 1] <= 0x7E)))
                    {
                        gbchars++;
                        totalfreq += 500;
                        row = rawtext[i] + 256 - 0x81;
                        if (0x40 <= rawtext[i + 1] && rawtext[i + 1] <= 0x7E)
                        {
                            column = rawtext[i + 1] - 0x40;
                        }
                        else
                        {
                            column = rawtext[i + 1] + 256 - 0x80;
                        }

                        if (GbkFreq[row][column] != 0)
                        {
                            gbfreq += GbkFreq[row][column];
                        }
                    }
                    i++;
                }
            }

            rangeval = 50 * (gbchars / dbchars);
            freqval = 50 * (gbfreq / totalfreq);

            return (int)(rangeval + freqval) - 1;
        }

        #endregion

        #region HZProbability.....

        /// <summary>
        /// 判断是HZ编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int HzProbability(sbyte[] rawtext)
        {
            int i, rawtextlen;
            long hzfreq = 0, totalfreq = 1;
            float rangeval, freqval;
            int hzstart = 0;

            rawtextlen = rawtext.Length;

            for (i = 0; i < rawtextlen; i++)
            {
                if (rawtext[i] == '~')
                {
                    if (rawtext[i + 1] == '{')
                    {
                        hzstart++;
                        i += 2;
                        while (i < rawtextlen - 1)
                        {
                            if (rawtext[i] == 0x0A || rawtext[i] == 0x0D)
                            {
                                break;
                            }
                            if (rawtext[i] == '~' && rawtext[i + 1] == '}')
                            {
                                i++;
                                break;
                            }
                            int row;
                            int column;
                            if ((0x21 <= rawtext[i] && rawtext[i] <= 0x77) && (0x21 <= rawtext[i + 1] && rawtext[i + 1] <= 0x77))
                            {
                                row = rawtext[i] - 0x21;
                                column = rawtext[i + 1] - 0x21;
                                totalfreq += 500;
                                if (GbFreq[row][column] != 0)
                                {
                                    hzfreq += GbFreq[row][column];
                                }
                                else if (15 <= row && row < 55)
                                {
                                    hzfreq += 200;
                                }
                            }
                            else
                            {
                            }
                            i += 2;
                        }
                    }
                    else if (rawtext[i + 1] == '}')
                    {
                        i++;
                    }
                    else if (rawtext[i + 1] == '~')
                    {
                        i++;
                    }
                }
            }

            if (hzstart > 4)
            {
                rangeval = 50;
            }
            else if (hzstart > 1)
            {
                rangeval = 41;
            }
            else if (hzstart > 0)
            {
                // Only 39 in case the sequence happened to occur
                rangeval = 39; // in otherwise non-Hz text
            }
            else
            {
                rangeval = 0;
            }
            freqval = 50 * (hzfreq / (float)totalfreq);


            return (int)(rangeval + freqval);
        }

        #endregion

        #region BIG5Probability.....

        /// <summary>
        /// 判断是BIG5编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int Big5Probability(sbyte[] rawtext)
        {
            int i, rawtextlen;
            int dbchars = 1, bfchars = 1;
            float rangeval, freqval;
            long bffreq = 0, totalfreq = 1;

            // Check to see if characters fit into acceptable ranges

            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen - 1; i++)
            {
                if (rawtext[i] >= 0)
                {
                    //asciichars++;
                }
                else
                {
                    dbchars++;
                    if ((sbyte)Identity(0xA1) <= rawtext[i] && rawtext[i] <= (sbyte)Identity(0xF9) && (0x40 <= rawtext[i + 1] && rawtext[i + 1] <= 0x7E || ((sbyte)Identity(0xA1) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xFE))))
                    {
                        bfchars++;
                        totalfreq += 500;
                        var row = rawtext[i] + 256 - 0xA1;
                        int column;
                        if (0x40 <= rawtext[i + 1] && rawtext[i + 1] <= 0x7E)
                        {
                            column = rawtext[i + 1] - 0x40;
                        }
                        else
                        {
                            column = rawtext[i + 1] + 256 - 0x61;
                        }
                        if (Big5Freq[row][column] != 0)
                        {
                            bffreq += Big5Freq[row][column];
                        }
                        else if (3 <= row && row <= 37)
                        {
                            bffreq += 200;
                        }
                    }
                    i++;
                }
            }

            rangeval = 50 * (bfchars / (float)dbchars);
            freqval = 50 * (bffreq / (float)totalfreq);


            return (int)(rangeval + freqval);
        }

        #endregion

        #region ENCTWProbability.....

        /// <summary>
        /// 判断是CNS11643(台湾)编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int EnctwProbability(sbyte[] rawtext)
        {
            int i, rawtextlen;
            int dbchars = 1, cnschars = 1;
            long cnsfreq = 0, totalfreq = 1;
            float rangeval, freqval;

            // Check to see if characters fit into acceptable ranges
            // and have expected frequency of use

            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen - 1; i++)
            {
                if (rawtext[i] >= 0)
                {
                    // in ASCII range
                    //asciichars++;
                }
                else
                {
                    // high bit set
                    dbchars++;
                    if (i + 3 < rawtextlen && (sbyte)Identity(0x8E) == rawtext[i] && (sbyte)Identity(0xA1) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xB0) && (sbyte)Identity(0xA1) <= rawtext[i + 2] && rawtext[i + 2] <= (sbyte)Identity(0xFE) && (sbyte)Identity(0xA1) <= rawtext[i + 3] && rawtext[i + 3] <= (sbyte)Identity(0xFE))
                    {
                        // Planes 1 - 16

                        cnschars++;
                        // These are all less frequent chars so just ignore freq
                        i += 3;
                    }
                    else if ((sbyte)Identity(0xA1) <= rawtext[i] && rawtext[i] <= (sbyte)Identity(0xFE) && (sbyte)Identity(0xA1) <= rawtext[i + 1] && rawtext[i + 1] <= (sbyte)Identity(0xFE))
                    {
                        cnschars++;
                        totalfreq += 500;
                        var row = rawtext[i] + 256 - 0xA1;
                        var column = rawtext[i + 1] + 256 - 0xA1;
                        if (EucTwFreq[row][column] != 0)
                        {
                            cnsfreq += EucTwFreq[row][column];
                        }
                        else if (35 <= row && row <= 92)
                        {
                            cnsfreq += 150;
                        }
                        i++;
                    }
                }
            }


            rangeval = 50 * (cnschars / (float)dbchars);
            freqval = 50 * (cnsfreq / (float)totalfreq);


            return (int)(rangeval + freqval);
        }

        #endregion

        #region ISO2022CNProbability.....

        /// <summary>
        /// 判断是ISO2022CN编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int Iso2022CnProbability(sbyte[] rawtext)
        {
            int i, rawtextlen;
            int dbchars = 1, isochars = 1;
            long isofreq = 0, totalfreq = 1;
            float rangeval, freqval;

            // Check to see if characters fit into acceptable ranges
            // and have expected frequency of use

            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen - 1; i++)
            {
                if (rawtext[i] == 0x1B && i + 3 < rawtextlen)
                {
                    // Escape char ESC
                    int row;
                    int column;
                    if (rawtext[i + 1] == 0x24 && rawtext[i + 2] == 0x29 && rawtext[i + 3] == 0x41)
                    {
                        // GB Escape  $ ) A
                        i += 4;
                        while (rawtext[i] != 0x1B)
                        {
                            dbchars++;
                            if ((0x21 <= rawtext[i] && rawtext[i] <= 0x77) && (0x21 <= rawtext[i + 1] && rawtext[i + 1] <= 0x77))
                            {
                                isochars++;
                                row = rawtext[i] - 0x21;
                                column = rawtext[i + 1] - 0x21;
                                totalfreq += 500;
                                if (GbFreq[row][column] != 0)
                                {
                                    isofreq += GbFreq[row][column];
                                }
                                else if (15 <= row && row < 55)
                                {
                                    isofreq += 200;
                                }
                                i++;
                            }
                            i++;
                        }
                    }
                    else if (i + 3 < rawtextlen && rawtext[i + 1] == 0x24 && rawtext[i + 2] == 0x29 && rawtext[i + 3] == 0x47)
                    {
                        // CNS Escape $ ) G
                        i += 4;
                        while (rawtext[i] != 0x1B)
                        {
                            dbchars++;
                            if (0x21 <= rawtext[i] && rawtext[i] <= 0x7E && 0x21 <= rawtext[i + 1] && rawtext[i + 1] <= 0x7E)
                            {
                                isochars++;
                                totalfreq += 500;
                                row = rawtext[i] - 0x21;
                                column = rawtext[i + 1] - 0x21;
                                if (EucTwFreq[row][column] != 0)
                                {
                                    isofreq += EucTwFreq[row][column];
                                }
                                else if (35 <= row && row <= 92)
                                {
                                    isofreq += 150;
                                }
                                i++;
                            }
                            i++;
                        }
                    }
                    if (rawtext[i] == 0x1B && i + 2 < rawtextlen && rawtext[i + 1] == 0x28 && rawtext[i + 2] == 0x42)
                    {
                        // ASCII:  ESC ( B
                        i += 2;
                    }
                }
            }

            rangeval = 50 * (isochars / (float)dbchars);
            freqval = 50 * (isofreq / (float)totalfreq);

            return (int)(rangeval + freqval);
        }

        #endregion

        #region UTF8Probability.....

        /// <summary>
        /// 判断是UTF8编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int Utf8Probability(sbyte[] rawtext)
        {
            int score;
            int i, rawtextlen;
            int goodbytes = 0, asciibytes = 0;

            // Maybe also use UTF8 Byte order Mark:  EF BB BF

            // Check to see if characters fit into acceptable ranges
            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen; i++)
            {
                if ((rawtext[i] & 0x7F) == rawtext[i])
                {
                    // One byte
                    asciibytes++;
                    // Ignore ASCII, can throw off count
                }
                else if (-64 <= rawtext[i] && rawtext[i] <= -33 && i + 1 < rawtextlen && -128 <= rawtext[i + 1] && rawtext[i + 1] <= -65)
                {
                    goodbytes += 2;
                    i++;
                }
                else if (-32 <= rawtext[i] && rawtext[i] <= -17 && i + 2 < rawtextlen && -128 <= rawtext[i + 1] && rawtext[i + 1] <= -65 && -128 <= rawtext[i + 2] && rawtext[i + 2] <= -65)
                {
                    goodbytes += 3;
                    i += 2;
                }
            }

            if (asciibytes == rawtextlen)
            {
                return 0;
            }

            score = (int)(100 * (goodbytes / (float)(rawtextlen - asciibytes)));

            // If not above 98, reduce to zero to prevent coincidental matches
            // Allows for some (few) bad formed sequences
            if (score > 98)
            {
                return score;
            }
            else if (score > 95 && goodbytes > 30)
            {
                return score;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region UnicodeProbability.....

        /// <summary>
        /// 判断是Unicode编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int UnicodeProbability(sbyte[] rawtext)
        {
            //int score = 0;
            //int i, rawtextlen = 0;
            //int goodbytes = 0, asciibytes = 0;

            if (((sbyte)Identity(0xFE) == rawtext[0] && (sbyte)Identity(0xFF) == rawtext[1]) || ((sbyte)Identity(0xFF) == rawtext[0] && (sbyte)Identity(0xFE) == rawtext[1]))
            {
                return 100;
            }

            return 0;
        }

        #endregion

        #region ASCIIProbability.....

        /// <summary>
        /// 判断是ASCII编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        internal int AsciiProbability(sbyte[] rawtext)
        {
            int score = 70;
            int i, rawtextlen;

            rawtextlen = rawtext.Length;

            for (i = 0; i < rawtextlen; i++)
            {
                if (rawtext[i] < 0)
                {
                    score = score - 5;
                }
                else if (rawtext[i] == 0x1B)
                {
                    // ESC (used by ISO 2022)
                    score = score - 5;
                }
            }

            return score;
        }

        #endregion

        #endregion

        #region Initialize_Frequencies.....

        /// <summary>
        /// 初始化必要的条件
        /// </summary>
        internal void Initialize_Frequencies()
        {
            int i;
            if (GbFreq[0] == null)
            {
                for (i = 0; i < 94; i++)
                {
                    GbFreq[i] = new int[94];
                }

                #region GBFreq[20][35] = 599;

                GbFreq[49][26] = 598;
                GbFreq[41][38] = 597;
                GbFreq[17][26] = 596;
                GbFreq[32][42] = 595;
                GbFreq[39][42] = 594;
                GbFreq[45][49] = 593;
                GbFreq[51][57] = 592;
                GbFreq[50][47] = 591;
                GbFreq[42][90] = 590;
                GbFreq[52][65] = 589;
                GbFreq[53][47] = 588;
                GbFreq[19][82] = 587;
                GbFreq[31][19] = 586;
                GbFreq[40][46] = 585;
                GbFreq[24][89] = 584;
                GbFreq[23][85] = 583;
                GbFreq[20][28] = 582;
                GbFreq[42][20] = 581;
                GbFreq[34][38] = 580;
                GbFreq[45][9] = 579;
                GbFreq[54][50] = 578;
                GbFreq[25][44] = 577;
                GbFreq[35][66] = 576;
                GbFreq[20][55] = 575;
                GbFreq[18][85] = 574;
                GbFreq[20][31] = 573;
                GbFreq[49][17] = 572;
                GbFreq[41][16] = 571;
                GbFreq[35][73] = 570;
                GbFreq[20][34] = 569;
                GbFreq[29][44] = 568;
                GbFreq[35][38] = 567;
                GbFreq[49][9] = 566;
                GbFreq[46][33] = 565;
                GbFreq[49][51] = 564;
                GbFreq[40][89] = 563;
                GbFreq[26][64] = 562;
                GbFreq[54][51] = 561;
                GbFreq[54][36] = 560;
                GbFreq[39][4] = 559;
                GbFreq[53][13] = 558;
                GbFreq[24][92] = 557;
                GbFreq[27][49] = 556;
                GbFreq[48][6] = 555;
                GbFreq[21][51] = 554;
                GbFreq[30][40] = 553;
                GbFreq[42][92] = 552;
                GbFreq[31][78] = 551;
                GbFreq[25][82] = 550;
                GbFreq[47][0] = 549;
                GbFreq[34][19] = 548;
                GbFreq[47][35] = 547;
                GbFreq[21][63] = 546;
                GbFreq[43][75] = 545;
                GbFreq[21][87] = 544;
                GbFreq[35][59] = 543;
                GbFreq[25][34] = 542;
                GbFreq[21][27] = 541;
                GbFreq[39][26] = 540;
                GbFreq[34][26] = 539;
                GbFreq[39][52] = 538;
                GbFreq[50][57] = 537;
                GbFreq[37][79] = 536;
                GbFreq[26][24] = 535;
                GbFreq[22][1] = 534;
                GbFreq[18][40] = 533;
                GbFreq[41][33] = 532;
                GbFreq[53][26] = 531;
                GbFreq[54][86] = 530;
                GbFreq[20][16] = 529;
                GbFreq[46][74] = 528;
                GbFreq[30][19] = 527;
                GbFreq[45][35] = 526;
                GbFreq[45][61] = 525;
                GbFreq[30][9] = 524;
                GbFreq[41][53] = 523;
                GbFreq[41][13] = 522;
                GbFreq[50][34] = 521;
                GbFreq[53][86] = 520;
                GbFreq[47][47] = 519;
                GbFreq[22][28] = 518;
                GbFreq[50][53] = 517;
                GbFreq[39][70] = 516;
                GbFreq[38][15] = 515;
                GbFreq[42][88] = 514;
                GbFreq[16][29] = 513;
                GbFreq[27][90] = 512;
                GbFreq[29][12] = 511;
                GbFreq[44][22] = 510;
                GbFreq[34][69] = 509;
                GbFreq[24][10] = 508;
                GbFreq[44][11] = 507;
                GbFreq[39][92] = 506;
                GbFreq[49][48] = 505;
                GbFreq[31][46] = 504;
                GbFreq[19][50] = 503;
                GbFreq[21][14] = 502;
                GbFreq[32][28] = 501;
                GbFreq[18][3] = 500;
                GbFreq[53][9] = 499;
                GbFreq[34][80] = 498;
                GbFreq[48][88] = 497;
                GbFreq[46][53] = 496;
                GbFreq[22][53] = 495;
                GbFreq[28][10] = 494;
                GbFreq[44][65] = 493;
                GbFreq[20][10] = 492;
                GbFreq[40][76] = 491;
                GbFreq[47][8] = 490;
                GbFreq[50][74] = 489;
                GbFreq[23][62] = 488;
                GbFreq[49][65] = 487;
                GbFreq[28][87] = 486;
                GbFreq[15][48] = 485;
                GbFreq[22][7] = 484;
                GbFreq[19][42] = 483;
                GbFreq[41][20] = 482;
                GbFreq[26][55] = 481;
                GbFreq[21][93] = 480;
                GbFreq[31][76] = 479;
                GbFreq[34][31] = 478;
                GbFreq[20][66] = 477;
                GbFreq[51][33] = 476;
                GbFreq[34][86] = 475;
                GbFreq[37][67] = 474;
                GbFreq[53][53] = 473;
                GbFreq[40][88] = 472;
                GbFreq[39][10] = 471;
                GbFreq[24][3] = 470;
                GbFreq[27][25] = 469;
                GbFreq[26][15] = 468;
                GbFreq[21][88] = 467;
                GbFreq[52][62] = 466;
                GbFreq[46][81] = 465;
                GbFreq[38][72] = 464;
                GbFreq[17][30] = 463;
                GbFreq[52][92] = 462;
                GbFreq[34][90] = 461;
                GbFreq[21][7] = 460;
                GbFreq[36][13] = 459;
                GbFreq[45][41] = 458;
                GbFreq[32][5] = 457;
                GbFreq[26][89] = 456;
                GbFreq[23][87] = 455;
                GbFreq[20][39] = 454;
                GbFreq[27][23] = 453;
                GbFreq[25][59] = 452;
                GbFreq[49][20] = 451;
                GbFreq[54][77] = 450;
                GbFreq[27][67] = 449;
                GbFreq[47][33] = 448;
                GbFreq[41][17] = 447;
                GbFreq[19][81] = 446;
                GbFreq[16][66] = 445;
                GbFreq[45][26] = 444;
                GbFreq[49][81] = 443;
                GbFreq[53][55] = 442;
                GbFreq[16][26] = 441;
                GbFreq[54][62] = 440;
                GbFreq[20][70] = 439;
                GbFreq[42][35] = 438;
                GbFreq[20][57] = 437;
                GbFreq[34][36] = 436;
                GbFreq[46][63] = 435;
                GbFreq[19][45] = 434;
                GbFreq[21][10] = 433;
                GbFreq[52][93] = 432;
                GbFreq[25][2] = 431;
                GbFreq[30][57] = 430;
                GbFreq[41][24] = 429;
                GbFreq[28][43] = 428;
                GbFreq[45][86] = 427;
                GbFreq[51][56] = 426;
                GbFreq[37][28] = 425;
                GbFreq[52][69] = 424;
                GbFreq[43][92] = 423;
                GbFreq[41][31] = 422;
                GbFreq[37][87] = 421;
                GbFreq[47][36] = 420;
                GbFreq[16][16] = 419;
                GbFreq[40][56] = 418;
                GbFreq[24][55] = 417;
                GbFreq[17][1] = 416;
                GbFreq[35][57] = 415;
                GbFreq[27][50] = 414;
                GbFreq[26][14] = 413;
                GbFreq[50][40] = 412;
                GbFreq[39][19] = 411;
                GbFreq[19][89] = 410;
                GbFreq[29][91] = 409;
                GbFreq[17][89] = 408;
                GbFreq[39][74] = 407;
                GbFreq[46][39] = 406;
                GbFreq[40][28] = 405;
                GbFreq[45][68] = 404;
                GbFreq[43][10] = 403;
                GbFreq[42][13] = 402;
                GbFreq[44][81] = 401;
                GbFreq[41][47] = 400;
                GbFreq[48][58] = 399;
                GbFreq[43][68] = 398;
                GbFreq[16][79] = 397;
                GbFreq[19][5] = 396;
                GbFreq[54][59] = 395;
                GbFreq[17][36] = 394;
                GbFreq[18][0] = 393;
                GbFreq[41][5] = 392;
                GbFreq[41][72] = 391;
                GbFreq[16][39] = 390;
                GbFreq[54][0] = 389;
                GbFreq[51][16] = 388;
                GbFreq[29][36] = 387;
                GbFreq[47][5] = 386;
                GbFreq[47][51] = 385;
                GbFreq[44][7] = 384;
                GbFreq[35][30] = 383;
                GbFreq[26][9] = 382;
                GbFreq[16][7] = 381;
                GbFreq[32][1] = 380;
                GbFreq[33][76] = 379;
                GbFreq[34][91] = 378;
                GbFreq[52][36] = 377;
                GbFreq[26][77] = 376;
                GbFreq[35][48] = 375;
                GbFreq[40][80] = 374;
                GbFreq[41][92] = 373;
                GbFreq[27][93] = 372;
                GbFreq[15][17] = 371;
                GbFreq[16][76] = 370;
                GbFreq[51][12] = 369;
                GbFreq[18][20] = 368;
                GbFreq[15][54] = 367;
                GbFreq[50][5] = 366;
                GbFreq[33][22] = 365;
                GbFreq[37][57] = 364;
                GbFreq[28][47] = 363;
                GbFreq[42][31] = 362;
                GbFreq[18][2] = 361;
                GbFreq[43][64] = 360;
                GbFreq[23][47] = 359;
                GbFreq[28][79] = 358;
                GbFreq[25][45] = 357;
                GbFreq[23][91] = 356;
                GbFreq[22][19] = 355;
                GbFreq[25][46] = 354;
                GbFreq[22][36] = 353;
                GbFreq[54][85] = 352;
                GbFreq[46][20] = 351;
                GbFreq[27][37] = 350;
                GbFreq[26][81] = 349;
                GbFreq[42][29] = 348;
                GbFreq[31][90] = 347;
                GbFreq[41][59] = 346;
                GbFreq[24][65] = 345;
                GbFreq[44][84] = 344;
                GbFreq[24][90] = 343;
                GbFreq[38][54] = 342;
                GbFreq[28][70] = 341;
                GbFreq[27][15] = 340;
                GbFreq[28][80] = 339;
                GbFreq[29][8] = 338;
                GbFreq[45][80] = 337;
                GbFreq[53][37] = 336;
                GbFreq[28][65] = 335;
                GbFreq[23][86] = 334;
                GbFreq[39][45] = 333;
                GbFreq[53][32] = 332;
                GbFreq[38][68] = 331;
                GbFreq[45][78] = 330;
                GbFreq[43][7] = 329;
                GbFreq[46][82] = 328;
                GbFreq[27][38] = 327;
                GbFreq[16][62] = 326;
                GbFreq[24][17] = 325;
                GbFreq[22][70] = 324;
                GbFreq[52][28] = 323;
                GbFreq[23][40] = 322;
                GbFreq[28][50] = 321;
                GbFreq[42][91] = 320;
                GbFreq[47][76] = 319;
                GbFreq[15][42] = 318;
                GbFreq[43][55] = 317;
                GbFreq[29][84] = 316;
                GbFreq[44][90] = 315;
                GbFreq[53][16] = 314;
                GbFreq[22][93] = 313;
                GbFreq[34][10] = 312;
                GbFreq[32][53] = 311;
                GbFreq[43][65] = 310;
                GbFreq[28][7] = 309;
                GbFreq[35][46] = 308;
                GbFreq[21][39] = 307;
                GbFreq[44][18] = 306;
                GbFreq[40][10] = 305;
                GbFreq[54][53] = 304;
                GbFreq[38][74] = 303;
                GbFreq[28][26] = 302;
                GbFreq[15][13] = 301;
                GbFreq[39][34] = 300;
                GbFreq[39][46] = 299;
                GbFreq[42][66] = 298;
                GbFreq[33][58] = 297;
                GbFreq[15][56] = 296;
                GbFreq[18][51] = 295;
                GbFreq[49][68] = 294;
                GbFreq[30][37] = 293;
                GbFreq[51][84] = 292;
                GbFreq[51][9] = 291;
                GbFreq[40][70] = 290;
                GbFreq[41][84] = 289;
                GbFreq[28][64] = 288;
                GbFreq[32][88] = 287;
                GbFreq[24][5] = 286;
                GbFreq[53][23] = 285;
                GbFreq[42][27] = 284;
                GbFreq[22][38] = 283;
                GbFreq[32][86] = 282;
                GbFreq[34][30] = 281;
                GbFreq[38][63] = 280;
                GbFreq[24][59] = 279;
                GbFreq[22][81] = 278;
                GbFreq[32][11] = 277;
                GbFreq[51][21] = 276;
                GbFreq[54][41] = 275;
                GbFreq[21][50] = 274;
                GbFreq[23][89] = 273;
                GbFreq[19][87] = 272;
                GbFreq[26][7] = 271;
                GbFreq[30][75] = 270;
                GbFreq[43][84] = 269;
                GbFreq[51][25] = 268;
                GbFreq[16][67] = 267;
                GbFreq[32][9] = 266;
                GbFreq[48][51] = 265;
                GbFreq[39][7] = 264;
                GbFreq[44][88] = 263;
                GbFreq[52][24] = 262;
                GbFreq[23][34] = 261;
                GbFreq[32][75] = 260;
                GbFreq[19][10] = 259;
                GbFreq[28][91] = 258;
                GbFreq[32][83] = 257;
                GbFreq[25][75] = 256;
                GbFreq[53][45] = 255;
                GbFreq[29][85] = 254;
                GbFreq[53][59] = 253;
                GbFreq[16][2] = 252;
                GbFreq[19][78] = 251;
                GbFreq[15][75] = 250;
                GbFreq[51][42] = 249;
                GbFreq[45][67] = 248;
                GbFreq[15][74] = 247;
                GbFreq[25][81] = 246;
                GbFreq[37][62] = 245;
                GbFreq[16][55] = 244;
                GbFreq[18][38] = 243;
                GbFreq[23][23] = 242;

                GbFreq[38][30] = 241;
                GbFreq[17][28] = 240;
                GbFreq[44][73] = 239;
                GbFreq[23][78] = 238;
                GbFreq[40][77] = 237;
                GbFreq[38][87] = 236;
                GbFreq[27][19] = 235;
                GbFreq[38][82] = 234;
                GbFreq[37][22] = 233;
                GbFreq[41][30] = 232;
                GbFreq[54][9] = 231;
                GbFreq[32][30] = 230;
                GbFreq[30][52] = 229;
                GbFreq[40][84] = 228;
                GbFreq[53][57] = 227;
                GbFreq[27][27] = 226;
                GbFreq[38][64] = 225;
                GbFreq[18][43] = 224;
                GbFreq[23][69] = 223;
                GbFreq[28][12] = 222;
                GbFreq[50][78] = 221;
                GbFreq[50][1] = 220;
                GbFreq[26][88] = 219;
                GbFreq[36][40] = 218;
                GbFreq[33][89] = 217;
                GbFreq[41][28] = 216;
                GbFreq[31][77] = 215;
                GbFreq[46][1] = 214;
                GbFreq[47][19] = 213;
                GbFreq[35][55] = 212;
                GbFreq[41][21] = 211;
                GbFreq[27][10] = 210;
                GbFreq[32][77] = 209;
                GbFreq[26][37] = 208;
                GbFreq[20][33] = 207;
                GbFreq[41][52] = 206;
                GbFreq[32][18] = 205;
                GbFreq[38][13] = 204;
                GbFreq[20][18] = 203;
                GbFreq[20][24] = 202;
                GbFreq[45][19] = 201;
                GbFreq[18][53] = 200;

                #endregion
            }

            if (GbkFreq[0] == null)
            {
                for (i = 0; i < 126; i++)
                {
                    GbkFreq[i] = new int[191];
                }

                #region GBKFreq[52][132] = 600;

                GbkFreq[73][135] = 599;
                GbkFreq[49][123] = 598;
                GbkFreq[77][146] = 597;
                GbkFreq[81][123] = 596;
                GbkFreq[82][144] = 595;
                GbkFreq[51][179] = 594;
                GbkFreq[83][154] = 593;
                GbkFreq[71][139] = 592;
                GbkFreq[64][139] = 591;
                GbkFreq[85][144] = 590;
                GbkFreq[52][125] = 589;
                GbkFreq[88][25] = 588;
                GbkFreq[81][106] = 587;
                GbkFreq[81][148] = 586;
                GbkFreq[62][137] = 585;
                GbkFreq[94][0] = 584;
                GbkFreq[1][64] = 583;
                GbkFreq[67][163] = 582;
                GbkFreq[20][190] = 581;
                GbkFreq[57][131] = 580;
                GbkFreq[29][169] = 579;
                GbkFreq[72][143] = 578;
                var ints = GbkFreq[0];
                if (ints != null) ints[173] = 577;
                GbkFreq[11][23] = 576;
                GbkFreq[61][141] = 575;
                GbkFreq[60][123] = 574;
                GbkFreq[81][114] = 573;
                GbkFreq[82][131] = 572;
                GbkFreq[67][156] = 571;
                GbkFreq[71][167] = 570;
                GbkFreq[20][50] = 569;
                GbkFreq[77][132] = 568;
                GbkFreq[84][38] = 567;
                GbkFreq[26][29] = 566;
                GbkFreq[74][187] = 565;
                GbkFreq[62][116] = 564;
                GbkFreq[67][135] = 563;
                GbkFreq[5][86] = 562;
                GbkFreq[72][186] = 561;
                GbkFreq[75][161] = 560;
                GbkFreq[78][130] = 559;
                GbkFreq[94][30] = 558;
                GbkFreq[84][72] = 557;
                GbkFreq[1][67] = 556;
                GbkFreq[75][172] = 555;
                GbkFreq[74][185] = 554;
                GbkFreq[53][160] = 553;
                GbkFreq[123][14] = 552;
                GbkFreq[79][97] = 551;
                GbkFreq[85][110] = 550;
                GbkFreq[78][171] = 549;
                GbkFreq[52][131] = 548;
                GbkFreq[56][100] = 547;
                GbkFreq[50][182] = 546;
                GbkFreq[94][64] = 545;
                GbkFreq[106][74] = 544;
                GbkFreq[11][102] = 543;
                GbkFreq[53][124] = 542;
                GbkFreq[24][3] = 541;
                GbkFreq[86][148] = 540;
                GbkFreq[53][184] = 539;
                GbkFreq[86][147] = 538;
                GbkFreq[96][161] = 537;
                GbkFreq[82][77] = 536;
                GbkFreq[59][146] = 535;
                GbkFreq[84][126] = 534;
                GbkFreq[79][132] = 533;
                GbkFreq[85][123] = 532;
                GbkFreq[71][101] = 531;
                GbkFreq[85][106] = 530;
                GbkFreq[6][184] = 529;
                GbkFreq[57][156] = 528;
                GbkFreq[75][104] = 527;
                GbkFreq[50][137] = 526;
                GbkFreq[79][133] = 525;
                GbkFreq[76][108] = 524;
                GbkFreq[57][142] = 523;
                GbkFreq[84][130] = 522;
                GbkFreq[52][128] = 521;
                GbkFreq[47][44] = 520;
                GbkFreq[52][152] = 519;
                GbkFreq[54][104] = 518;
                GbkFreq[30][47] = 517;
                GbkFreq[71][123] = 516;
                GbkFreq[52][107] = 515;
                GbkFreq[45][84] = 514;
                GbkFreq[107][118] = 513;
                GbkFreq[5][161] = 512;
                GbkFreq[48][126] = 511;
                GbkFreq[67][170] = 510;
                GbkFreq[43][6] = 509;
                GbkFreq[70][112] = 508;
                GbkFreq[86][174] = 507;
                GbkFreq[84][166] = 506;
                GbkFreq[79][130] = 505;
                GbkFreq[57][141] = 504;
                GbkFreq[81][178] = 503;
                GbkFreq[56][187] = 502;
                GbkFreq[81][162] = 501;
                GbkFreq[53][104] = 500;
                GbkFreq[123][35] = 499;
                GbkFreq[70][169] = 498;
                GbkFreq[69][164] = 497;
                GbkFreq[109][61] = 496;
                GbkFreq[73][130] = 495;
                GbkFreq[62][134] = 494;
                GbkFreq[54][125] = 493;
                GbkFreq[79][105] = 492;
                GbkFreq[70][165] = 491;
                GbkFreq[71][189] = 490;
                GbkFreq[23][147] = 489;
                GbkFreq[51][139] = 488;
                GbkFreq[47][137] = 487;
                GbkFreq[77][123] = 486;
                GbkFreq[86][183] = 485;
                GbkFreq[63][173] = 484;
                GbkFreq[79][144] = 483;
                GbkFreq[84][159] = 482;
                GbkFreq[60][91] = 481;
                GbkFreq[66][187] = 480;
                GbkFreq[73][114] = 479;
                GbkFreq[85][56] = 478;
                GbkFreq[71][149] = 477;
                GbkFreq[84][189] = 476;
                GbkFreq[104][31] = 475;
                GbkFreq[83][82] = 474;
                GbkFreq[68][35] = 473;
                GbkFreq[11][77] = 472;
                GbkFreq[15][155] = 471;
                GbkFreq[83][153] = 470;
                GbkFreq[71][1] = 469;
                GbkFreq[53][190] = 468;
                GbkFreq[50][135] = 467;
                GbkFreq[3][147] = 466;
                GbkFreq[48][136] = 465;
                GbkFreq[66][166] = 464;
                GbkFreq[55][159] = 463;
                GbkFreq[82][150] = 462;
                GbkFreq[58][178] = 461;
                GbkFreq[64][102] = 460;
                GbkFreq[16][106] = 459;
                GbkFreq[68][110] = 458;
                GbkFreq[54][14] = 457;
                GbkFreq[60][140] = 456;
                GbkFreq[91][71] = 455;
                GbkFreq[54][150] = 454;
                GbkFreq[78][177] = 453;
                GbkFreq[78][117] = 452;
                GbkFreq[104][12] = 451;
                GbkFreq[73][150] = 450;
                GbkFreq[51][142] = 449;
                GbkFreq[81][145] = 448;
                GbkFreq[66][183] = 447;
                GbkFreq[51][178] = 446;
                GbkFreq[75][107] = 445;
                GbkFreq[65][119] = 444;
                GbkFreq[69][176] = 443;
                GbkFreq[59][122] = 442;
                GbkFreq[78][160] = 441;
                GbkFreq[85][183] = 440;
                GbkFreq[105][16] = 439;
                GbkFreq[73][110] = 438;
                GbkFreq[104][39] = 437;
                GbkFreq[119][16] = 436;
                GbkFreq[76][162] = 435;
                GbkFreq[67][152] = 434;
                GbkFreq[82][24] = 433;
                GbkFreq[73][121] = 432;
                GbkFreq[83][83] = 431;
                GbkFreq[82][145] = 430;
                GbkFreq[49][133] = 429;
                GbkFreq[94][13] = 428;
                GbkFreq[58][139] = 427;
                GbkFreq[74][189] = 426;
                GbkFreq[66][177] = 425;
                GbkFreq[85][184] = 424;
                GbkFreq[55][183] = 423;
                GbkFreq[71][107] = 422;
                GbkFreq[11][98] = 421;
                GbkFreq[72][153] = 420;
                GbkFreq[2][137] = 419;
                GbkFreq[59][147] = 418;
                GbkFreq[58][152] = 417;
                GbkFreq[55][144] = 416;
                GbkFreq[73][125] = 415;
                GbkFreq[52][154] = 414;
                GbkFreq[70][178] = 413;
                GbkFreq[79][148] = 412;
                GbkFreq[63][143] = 411;
                GbkFreq[50][140] = 410;
                GbkFreq[47][145] = 409;
                GbkFreq[48][123] = 408;
                GbkFreq[56][107] = 407;
                GbkFreq[84][83] = 406;
                GbkFreq[59][112] = 405;
                GbkFreq[124][72] = 404;
                GbkFreq[79][99] = 403;
                GbkFreq[3][37] = 402;
                GbkFreq[114][55] = 401;
                GbkFreq[85][152] = 400;
                GbkFreq[60][47] = 399;
                GbkFreq[65][96] = 398;
                GbkFreq[74][110] = 397;
                GbkFreq[86][182] = 396;
                GbkFreq[50][99] = 395;
                GbkFreq[67][186] = 394;
                GbkFreq[81][74] = 393;
                GbkFreq[80][37] = 392;
                GbkFreq[21][60] = 391;
                GbkFreq[110][12] = 390;
                GbkFreq[60][162] = 389;
                GbkFreq[29][115] = 388;
                GbkFreq[83][130] = 387;
                GbkFreq[52][136] = 386;
                GbkFreq[63][114] = 385;
                GbkFreq[49][127] = 384;
                GbkFreq[83][109] = 383;
                GbkFreq[66][128] = 382;
                GbkFreq[78][136] = 381;
                GbkFreq[81][180] = 380;
                GbkFreq[76][104] = 379;
                GbkFreq[56][156] = 378;
                GbkFreq[61][23] = 377;
                GbkFreq[4][30] = 376;
                GbkFreq[69][154] = 375;
                GbkFreq[100][37] = 374;
                GbkFreq[54][177] = 373;
                GbkFreq[23][119] = 372;
                GbkFreq[71][171] = 371;
                GbkFreq[84][146] = 370;
                GbkFreq[20][184] = 369;
                GbkFreq[86][76] = 368;
                GbkFreq[74][132] = 367;
                GbkFreq[47][97] = 366;
                GbkFreq[82][137] = 365;
                GbkFreq[94][56] = 364;
                GbkFreq[92][30] = 363;
                GbkFreq[19][117] = 362;
                GbkFreq[48][173] = 361;
                GbkFreq[2][136] = 360;
                GbkFreq[7][182] = 359;
                GbkFreq[74][188] = 358;
                GbkFreq[14][132] = 357;
                GbkFreq[62][172] = 356;
                GbkFreq[25][39] = 355;
                GbkFreq[85][129] = 354;
                GbkFreq[64][98] = 353;
                GbkFreq[67][127] = 352;
                GbkFreq[72][167] = 351;
                GbkFreq[57][143] = 350;
                GbkFreq[76][187] = 349;
                GbkFreq[83][181] = 348;
                GbkFreq[84][10] = 347;
                GbkFreq[55][166] = 346;
                GbkFreq[55][188] = 345;
                GbkFreq[13][151] = 344;
                GbkFreq[62][124] = 343;
                GbkFreq[53][136] = 342;
                GbkFreq[106][57] = 341;
                GbkFreq[47][166] = 340;
                GbkFreq[109][30] = 339;
                GbkFreq[78][114] = 338;
                GbkFreq[83][19] = 337;
                GbkFreq[56][162] = 336;
                GbkFreq[60][177] = 335;
                GbkFreq[88][9] = 334;
                GbkFreq[74][163] = 333;
                GbkFreq[52][156] = 332;
                GbkFreq[71][180] = 331;
                GbkFreq[60][57] = 330;
                GbkFreq[72][173] = 329;
                GbkFreq[82][91] = 328;
                GbkFreq[51][186] = 327;
                GbkFreq[75][86] = 326;
                GbkFreq[75][78] = 325;
                GbkFreq[76][170] = 324;
                GbkFreq[60][147] = 323;
                GbkFreq[82][75] = 322;
                GbkFreq[80][148] = 321;
                GbkFreq[86][150] = 320;
                GbkFreq[13][95] = 319;
                var ints1 = GbkFreq[0];
                if (ints1 != null) ints1[11] = 318;
                GbkFreq[84][190] = 317;
                GbkFreq[76][166] = 316;
                GbkFreq[14][72] = 315;
                GbkFreq[67][144] = 314;
                GbkFreq[84][44] = 313;
                GbkFreq[72][125] = 312;
                GbkFreq[66][127] = 311;
                GbkFreq[60][25] = 310;
                GbkFreq[70][146] = 309;
                GbkFreq[79][135] = 308;
                GbkFreq[54][135] = 307;
                GbkFreq[60][104] = 306;
                GbkFreq[55][132] = 305;
                GbkFreq[94][2] = 304;
                GbkFreq[54][133] = 303;
                GbkFreq[56][190] = 302;
                GbkFreq[58][174] = 301;
                GbkFreq[80][144] = 300;
                GbkFreq[85][113] = 299;

                #endregion
            }

            if (Big5Freq[0] == null)
            {
                for (i = 0; i < 94; i++)
                {
                    Big5Freq[i] = new int[158];
                }

                #region Big5Freq[9][89] = 600;

                Big5Freq[11][15] = 599;
                Big5Freq[3][66] = 598;
                Big5Freq[6][121] = 597;
                Big5Freq[3][0] = 596;
                Big5Freq[5][82] = 595;
                Big5Freq[3][42] = 594;
                Big5Freq[5][34] = 593;
                Big5Freq[3][8] = 592;
                Big5Freq[3][6] = 591;
                Big5Freq[3][67] = 590;
                Big5Freq[7][139] = 589;
                Big5Freq[23][137] = 588;
                Big5Freq[12][46] = 587;
                Big5Freq[4][8] = 586;
                Big5Freq[4][41] = 585;
                Big5Freq[18][47] = 584;
                Big5Freq[12][114] = 583;
                Big5Freq[6][1] = 582;
                Big5Freq[22][60] = 581;
                Big5Freq[5][46] = 580;
                Big5Freq[11][79] = 579;
                Big5Freq[3][23] = 578;
                Big5Freq[7][114] = 577;
                Big5Freq[29][102] = 576;
                Big5Freq[19][14] = 575;
                Big5Freq[4][133] = 574;
                Big5Freq[3][29] = 573;
                Big5Freq[4][109] = 572;
                Big5Freq[14][127] = 571;
                Big5Freq[5][48] = 570;
                Big5Freq[13][104] = 569;
                Big5Freq[3][132] = 568;
                Big5Freq[26][64] = 567;
                Big5Freq[7][19] = 566;
                Big5Freq[4][12] = 565;
                Big5Freq[11][124] = 564;
                Big5Freq[7][89] = 563;
                Big5Freq[15][124] = 562;
                Big5Freq[4][108] = 561;
                Big5Freq[19][66] = 560;
                Big5Freq[3][21] = 559;
                Big5Freq[24][12] = 558;
                Big5Freq[28][111] = 557;
                Big5Freq[12][107] = 556;
                Big5Freq[3][112] = 555;
                Big5Freq[8][113] = 554;
                Big5Freq[5][40] = 553;
                Big5Freq[26][145] = 552;
                Big5Freq[3][48] = 551;
                Big5Freq[3][70] = 550;
                Big5Freq[22][17] = 549;
                Big5Freq[16][47] = 548;
                Big5Freq[3][53] = 547;
                Big5Freq[4][24] = 546;
                Big5Freq[32][120] = 545;
                Big5Freq[24][49] = 544;
                Big5Freq[24][142] = 543;
                Big5Freq[18][66] = 542;
                Big5Freq[29][150] = 541;
                Big5Freq[5][122] = 540;
                Big5Freq[5][114] = 539;
                Big5Freq[3][44] = 538;
                Big5Freq[10][128] = 537;
                Big5Freq[15][20] = 536;
                Big5Freq[13][33] = 535;
                Big5Freq[14][87] = 534;
                Big5Freq[3][126] = 533;
                Big5Freq[4][53] = 532;
                Big5Freq[4][40] = 531;
                Big5Freq[9][93] = 530;
                Big5Freq[15][137] = 529;
                Big5Freq[10][123] = 528;
                Big5Freq[4][56] = 527;
                Big5Freq[5][71] = 526;
                Big5Freq[10][8] = 525;
                Big5Freq[5][16] = 524;
                Big5Freq[5][146] = 523;
                Big5Freq[18][88] = 522;
                Big5Freq[24][4] = 521;
                Big5Freq[20][47] = 520;
                Big5Freq[5][33] = 519;
                Big5Freq[9][43] = 518;
                Big5Freq[20][12] = 517;
                Big5Freq[20][13] = 516;
                Big5Freq[5][156] = 515;
                Big5Freq[22][140] = 514;
                Big5Freq[8][146] = 513;
                Big5Freq[21][123] = 512;
                Big5Freq[4][90] = 511;
                Big5Freq[5][62] = 510;
                Big5Freq[17][59] = 509;
                Big5Freq[10][37] = 508;
                Big5Freq[18][107] = 507;
                Big5Freq[14][53] = 506;
                Big5Freq[22][51] = 505;
                Big5Freq[8][13] = 504;
                Big5Freq[5][29] = 503;
                Big5Freq[9][7] = 502;
                Big5Freq[22][14] = 501;
                Big5Freq[8][55] = 500;
                Big5Freq[33][9] = 499;
                Big5Freq[16][64] = 498;
                Big5Freq[7][131] = 497;
                Big5Freq[34][4] = 496;
                Big5Freq[7][101] = 495;
                Big5Freq[11][139] = 494;
                Big5Freq[3][135] = 493;
                Big5Freq[7][102] = 492;
                Big5Freq[17][13] = 491;
                Big5Freq[3][20] = 490;
                Big5Freq[27][106] = 489;
                Big5Freq[5][88] = 488;
                Big5Freq[6][33] = 487;
                Big5Freq[5][139] = 486;
                Big5Freq[6][0] = 485;
                Big5Freq[17][58] = 484;
                Big5Freq[5][133] = 483;
                Big5Freq[9][107] = 482;
                Big5Freq[23][39] = 481;
                Big5Freq[5][23] = 480;
                Big5Freq[3][79] = 479;
                Big5Freq[32][97] = 478;
                Big5Freq[3][136] = 477;
                Big5Freq[4][94] = 476;
                Big5Freq[21][61] = 475;
                Big5Freq[23][123] = 474;
                Big5Freq[26][16] = 473;
                Big5Freq[24][137] = 472;
                Big5Freq[22][18] = 471;
                Big5Freq[5][1] = 470;
                Big5Freq[20][119] = 469;
                Big5Freq[3][7] = 468;
                Big5Freq[10][79] = 467;
                Big5Freq[15][105] = 466;
                Big5Freq[3][144] = 465;
                Big5Freq[12][80] = 464;
                Big5Freq[15][73] = 463;
                Big5Freq[3][19] = 462;
                Big5Freq[8][109] = 461;
                Big5Freq[3][15] = 460;
                Big5Freq[31][82] = 459;
                Big5Freq[3][43] = 458;
                Big5Freq[25][119] = 457;
                Big5Freq[16][111] = 456;
                Big5Freq[7][77] = 455;
                Big5Freq[3][95] = 454;
                Big5Freq[24][82] = 453;
                Big5Freq[7][52] = 452;
                Big5Freq[9][151] = 451;
                Big5Freq[3][129] = 450;
                Big5Freq[5][87] = 449;
                Big5Freq[3][55] = 448;
                Big5Freq[8][153] = 447;
                Big5Freq[4][83] = 446;
                Big5Freq[3][114] = 445;
                Big5Freq[23][147] = 444;
                Big5Freq[15][31] = 443;
                Big5Freq[3][54] = 442;
                Big5Freq[11][122] = 441;
                Big5Freq[4][4] = 440;
                Big5Freq[34][149] = 439;
                Big5Freq[3][17] = 438;
                Big5Freq[21][64] = 437;
                Big5Freq[26][144] = 436;
                Big5Freq[4][62] = 435;
                Big5Freq[8][15] = 434;
                Big5Freq[35][80] = 433;
                Big5Freq[7][110] = 432;
                Big5Freq[23][114] = 431;
                Big5Freq[3][108] = 430;
                Big5Freq[3][62] = 429;
                Big5Freq[21][41] = 428;
                Big5Freq[15][99] = 427;
                Big5Freq[5][47] = 426;
                Big5Freq[4][96] = 425;
                Big5Freq[20][122] = 424;
                Big5Freq[5][21] = 423;
                Big5Freq[4][157] = 422;
                Big5Freq[16][14] = 421;
                Big5Freq[3][117] = 420;
                Big5Freq[7][129] = 419;
                Big5Freq[4][27] = 418;
                Big5Freq[5][30] = 417;
                Big5Freq[22][16] = 416;
                Big5Freq[5][64] = 415;
                Big5Freq[17][99] = 414;
                Big5Freq[17][57] = 413;
                Big5Freq[8][105] = 412;
                Big5Freq[5][112] = 411;
                Big5Freq[20][59] = 410;
                Big5Freq[6][129] = 409;
                Big5Freq[18][17] = 408;
                Big5Freq[3][92] = 407;
                Big5Freq[28][118] = 406;
                Big5Freq[3][109] = 405;
                Big5Freq[31][51] = 404;
                Big5Freq[13][116] = 403;
                Big5Freq[6][15] = 402;
                Big5Freq[36][136] = 401;
                Big5Freq[12][74] = 400;
                Big5Freq[20][88] = 399;
                Big5Freq[36][68] = 398;
                Big5Freq[3][147] = 397;
                Big5Freq[15][84] = 396;
                Big5Freq[16][32] = 395;
                Big5Freq[16][58] = 394;
                Big5Freq[7][66] = 393;
                Big5Freq[23][107] = 392;
                Big5Freq[9][6] = 391;
                Big5Freq[12][86] = 390;
                Big5Freq[23][112] = 389;
                Big5Freq[37][23] = 388;
                Big5Freq[3][138] = 387;
                Big5Freq[20][68] = 386;
                Big5Freq[15][116] = 385;
                Big5Freq[18][64] = 384;
                Big5Freq[12][139] = 383;
                Big5Freq[11][155] = 382;
                Big5Freq[4][156] = 381;
                Big5Freq[12][84] = 380;
                Big5Freq[18][49] = 379;
                Big5Freq[25][125] = 378;
                Big5Freq[25][147] = 377;
                Big5Freq[15][110] = 376;
                Big5Freq[19][96] = 375;
                Big5Freq[30][152] = 374;
                Big5Freq[6][31] = 373;
                Big5Freq[27][117] = 372;
                Big5Freq[3][10] = 371;
                Big5Freq[6][131] = 370;
                Big5Freq[13][112] = 369;
                Big5Freq[36][156] = 368;
                Big5Freq[4][60] = 367;
                Big5Freq[15][121] = 366;
                Big5Freq[4][112] = 365;
                Big5Freq[30][142] = 364;
                Big5Freq[23][154] = 363;
                Big5Freq[27][101] = 362;
                Big5Freq[9][140] = 361;
                Big5Freq[3][89] = 360;
                Big5Freq[18][148] = 359;
                Big5Freq[4][69] = 358;
                Big5Freq[16][49] = 357;
                Big5Freq[6][117] = 356;
                Big5Freq[36][55] = 355;
                Big5Freq[5][123] = 354;
                Big5Freq[4][126] = 353;
                Big5Freq[4][119] = 352;
                Big5Freq[9][95] = 351;
                Big5Freq[5][24] = 350;
                Big5Freq[16][133] = 349;
                Big5Freq[10][134] = 348;
                Big5Freq[26][59] = 347;
                Big5Freq[6][41] = 346;
                Big5Freq[6][146] = 345;
                Big5Freq[19][24] = 344;
                Big5Freq[5][113] = 343;
                Big5Freq[10][118] = 342;
                Big5Freq[34][151] = 341;
                Big5Freq[9][72] = 340;
                Big5Freq[31][25] = 339;
                Big5Freq[18][126] = 338;
                Big5Freq[18][28] = 337;
                Big5Freq[4][153] = 336;
                Big5Freq[3][84] = 335;
                Big5Freq[21][18] = 334;
                Big5Freq[25][129] = 333;
                Big5Freq[6][107] = 332;
                Big5Freq[12][25] = 331;
                Big5Freq[17][109] = 330;
                Big5Freq[7][76] = 329;
                Big5Freq[15][15] = 328;
                Big5Freq[4][14] = 327;
                Big5Freq[23][88] = 326;
                Big5Freq[18][2] = 325;
                Big5Freq[6][88] = 324;
                Big5Freq[16][84] = 323;
                Big5Freq[12][48] = 322;
                Big5Freq[7][68] = 321;
                Big5Freq[5][50] = 320;
                Big5Freq[13][54] = 319;
                Big5Freq[7][98] = 318;
                Big5Freq[11][6] = 317;
                Big5Freq[9][80] = 316;
                Big5Freq[16][41] = 315;
                Big5Freq[7][43] = 314;
                Big5Freq[28][117] = 313;
                Big5Freq[3][51] = 312;
                Big5Freq[7][3] = 311;
                Big5Freq[20][81] = 310;
                Big5Freq[4][2] = 309;
                Big5Freq[11][16] = 308;
                Big5Freq[10][4] = 307;
                Big5Freq[10][119] = 306;
                Big5Freq[6][142] = 305;
                Big5Freq[18][51] = 304;
                Big5Freq[8][144] = 303;
                Big5Freq[10][65] = 302;
                Big5Freq[11][64] = 301;
                Big5Freq[11][130] = 300;
                Big5Freq[9][92] = 299;
                Big5Freq[18][29] = 298;
                Big5Freq[18][78] = 297;
                Big5Freq[18][151] = 296;
                Big5Freq[33][127] = 295;
                Big5Freq[35][113] = 294;
                Big5Freq[10][155] = 293;
                Big5Freq[3][76] = 292;
                Big5Freq[36][123] = 291;
                Big5Freq[13][143] = 290;
                Big5Freq[5][135] = 289;
                Big5Freq[23][116] = 288;
                Big5Freq[6][101] = 287;
                Big5Freq[14][74] = 286;
                Big5Freq[7][153] = 285;
                Big5Freq[3][101] = 284;
                Big5Freq[9][74] = 283;
                Big5Freq[3][156] = 282;
                Big5Freq[4][147] = 281;
                Big5Freq[9][12] = 280;
                Big5Freq[18][133] = 279;
                Big5Freq[4][0] = 278;
                Big5Freq[7][155] = 277;
                Big5Freq[9][144] = 276;
                Big5Freq[23][49] = 275;
                Big5Freq[5][89] = 274;
                Big5Freq[10][11] = 273;
                Big5Freq[3][110] = 272;
                Big5Freq[3][40] = 271;
                Big5Freq[29][115] = 270;
                Big5Freq[9][100] = 269;
                Big5Freq[21][67] = 268;
                Big5Freq[23][145] = 267;
                Big5Freq[10][47] = 266;
                Big5Freq[4][31] = 265;
                Big5Freq[4][81] = 264;
                Big5Freq[22][62] = 263;
                Big5Freq[4][28] = 262;
                Big5Freq[27][39] = 261;
                Big5Freq[27][54] = 260;
                Big5Freq[32][46] = 259;
                Big5Freq[4][76] = 258;
                Big5Freq[26][15] = 257;
                Big5Freq[12][154] = 256;
                Big5Freq[9][150] = 255;
                Big5Freq[15][17] = 254;
                Big5Freq[5][129] = 253;
                Big5Freq[10][40] = 252;
                Big5Freq[13][37] = 251;
                Big5Freq[31][104] = 250;
                Big5Freq[3][152] = 249;
                Big5Freq[5][22] = 248;
                Big5Freq[8][48] = 247;
                Big5Freq[4][74] = 246;
                Big5Freq[6][17] = 245;
                Big5Freq[30][82] = 244;
                Big5Freq[4][116] = 243;
                Big5Freq[16][42] = 242;
                Big5Freq[5][55] = 241;
                Big5Freq[4][64] = 240;
                Big5Freq[14][19] = 239;
                Big5Freq[35][82] = 238;
                Big5Freq[30][139] = 237;
                Big5Freq[26][152] = 236;
                Big5Freq[32][32] = 235;
                Big5Freq[21][102] = 234;
                Big5Freq[10][131] = 233;
                Big5Freq[9][128] = 232;
                Big5Freq[3][87] = 231;
                Big5Freq[4][51] = 230;
                Big5Freq[10][15] = 229;
                Big5Freq[4][150] = 228;
                Big5Freq[7][4] = 227;
                Big5Freq[7][51] = 226;
                Big5Freq[7][157] = 225;
                Big5Freq[4][146] = 224;
                Big5Freq[4][91] = 223;
                Big5Freq[7][13] = 222;
                Big5Freq[17][116] = 221;
                Big5Freq[23][21] = 220;
                Big5Freq[5][106] = 219;
                Big5Freq[14][100] = 218;
                Big5Freq[10][152] = 217;
                Big5Freq[14][89] = 216;
                Big5Freq[6][138] = 215;
                Big5Freq[12][157] = 214;
                Big5Freq[10][102] = 213;
                Big5Freq[19][94] = 212;
                Big5Freq[7][74] = 211;
                Big5Freq[18][128] = 210;
                Big5Freq[27][111] = 209;
                Big5Freq[11][57] = 208;
                Big5Freq[3][131] = 207;
                Big5Freq[30][23] = 206;
                Big5Freq[30][126] = 205;
                Big5Freq[4][36] = 204;
                Big5Freq[26][124] = 203;
                Big5Freq[4][19] = 202;
                Big5Freq[9][152] = 201;

                #endregion
            }

            if (EucTwFreq[0] == null)
            {
                for (i = 0; i < 94; i++)
                {
                    EucTwFreq[i] = new int[94];
                }

                #region EUC_TWFreq[48][49] = 599;

                EucTwFreq[35][65] = 598;
                EucTwFreq[41][27] = 597;
                EucTwFreq[35][0] = 596;
                EucTwFreq[39][19] = 595;
                EucTwFreq[35][42] = 594;
                EucTwFreq[38][66] = 593;
                EucTwFreq[35][8] = 592;
                EucTwFreq[35][6] = 591;
                EucTwFreq[35][66] = 590;
                EucTwFreq[43][14] = 589;
                EucTwFreq[69][80] = 588;
                EucTwFreq[50][48] = 587;
                EucTwFreq[36][71] = 586;
                EucTwFreq[37][10] = 585;
                EucTwFreq[60][52] = 584;
                EucTwFreq[51][21] = 583;
                EucTwFreq[40][2] = 582;
                EucTwFreq[67][35] = 581;
                EucTwFreq[38][78] = 580;
                EucTwFreq[49][18] = 579;
                EucTwFreq[35][23] = 578;
                EucTwFreq[42][83] = 577;
                EucTwFreq[79][47] = 576;
                EucTwFreq[61][82] = 575;
                EucTwFreq[38][7] = 574;
                EucTwFreq[35][29] = 573;
                EucTwFreq[37][77] = 572;
                EucTwFreq[54][67] = 571;
                EucTwFreq[38][80] = 570;
                EucTwFreq[52][74] = 569;
                EucTwFreq[36][37] = 568;
                EucTwFreq[74][8] = 567;
                EucTwFreq[41][83] = 566;
                EucTwFreq[36][75] = 565;
                EucTwFreq[49][63] = 564;
                EucTwFreq[42][58] = 563;
                EucTwFreq[56][33] = 562;
                EucTwFreq[37][76] = 561;
                EucTwFreq[62][39] = 560;
                EucTwFreq[35][21] = 559;
                EucTwFreq[70][19] = 558;
                EucTwFreq[77][88] = 557;
                EucTwFreq[51][14] = 556;
                EucTwFreq[36][17] = 555;
                EucTwFreq[44][51] = 554;
                EucTwFreq[38][72] = 553;
                EucTwFreq[74][90] = 552;
                EucTwFreq[35][48] = 551;
                EucTwFreq[35][69] = 550;
                EucTwFreq[66][86] = 549;
                EucTwFreq[57][20] = 548;
                EucTwFreq[35][53] = 547;
                EucTwFreq[36][87] = 546;
                EucTwFreq[84][67] = 545;
                EucTwFreq[70][56] = 544;
                EucTwFreq[71][54] = 543;
                EucTwFreq[60][70] = 542;
                EucTwFreq[80][1] = 541;
                EucTwFreq[39][59] = 540;
                EucTwFreq[39][51] = 539;
                EucTwFreq[35][44] = 538;
                EucTwFreq[48][4] = 537;
                EucTwFreq[55][24] = 536;
                EucTwFreq[52][4] = 535;
                EucTwFreq[54][26] = 534;
                EucTwFreq[36][31] = 533;
                EucTwFreq[37][22] = 532;
                EucTwFreq[37][9] = 531;
                EucTwFreq[46][0] = 530;
                EucTwFreq[56][46] = 529;
                EucTwFreq[47][93] = 528;
                EucTwFreq[37][25] = 527;
                EucTwFreq[39][8] = 526;
                EucTwFreq[46][73] = 525;
                EucTwFreq[38][48] = 524;
                EucTwFreq[39][83] = 523;
                EucTwFreq[60][92] = 522;
                EucTwFreq[70][11] = 521;
                EucTwFreq[63][84] = 520;
                EucTwFreq[38][65] = 519;
                EucTwFreq[45][45] = 518;
                EucTwFreq[63][49] = 517;
                EucTwFreq[63][50] = 516;
                EucTwFreq[39][93] = 515;
                EucTwFreq[68][20] = 514;
                EucTwFreq[44][84] = 513;
                EucTwFreq[66][34] = 512;
                EucTwFreq[37][58] = 511;
                EucTwFreq[39][0] = 510;
                EucTwFreq[59][1] = 509;
                EucTwFreq[47][8] = 508;
                EucTwFreq[61][17] = 507;
                EucTwFreq[53][87] = 506;
                EucTwFreq[67][26] = 505;
                EucTwFreq[43][46] = 504;
                EucTwFreq[38][61] = 503;
                EucTwFreq[45][9] = 502;
                EucTwFreq[66][83] = 501;
                EucTwFreq[43][88] = 500;
                EucTwFreq[85][20] = 499;
                EucTwFreq[57][36] = 498;
                EucTwFreq[43][6] = 497;
                EucTwFreq[86][77] = 496;
                EucTwFreq[42][70] = 495;
                EucTwFreq[49][78] = 494;
                EucTwFreq[36][40] = 493;
                EucTwFreq[42][71] = 492;
                EucTwFreq[58][49] = 491;
                EucTwFreq[35][20] = 490;
                EucTwFreq[76][20] = 489;
                EucTwFreq[39][25] = 488;
                EucTwFreq[40][34] = 487;
                EucTwFreq[39][76] = 486;
                EucTwFreq[40][1] = 485;
                EucTwFreq[59][0] = 484;
                EucTwFreq[39][70] = 483;
                EucTwFreq[46][14] = 482;
                EucTwFreq[68][77] = 481;
                EucTwFreq[38][55] = 480;
                EucTwFreq[35][78] = 479;
                EucTwFreq[84][44] = 478;
                EucTwFreq[36][41] = 477;
                EucTwFreq[37][62] = 476;
                EucTwFreq[65][67] = 475;
                EucTwFreq[69][66] = 474;
                EucTwFreq[73][55] = 473;
                EucTwFreq[71][49] = 472;
                EucTwFreq[66][87] = 471;
                EucTwFreq[38][33] = 470;
                EucTwFreq[64][61] = 469;
                EucTwFreq[35][7] = 468;
                EucTwFreq[47][49] = 467;
                EucTwFreq[56][14] = 466;
                EucTwFreq[36][49] = 465;
                EucTwFreq[50][81] = 464;
                EucTwFreq[55][76] = 463;
                EucTwFreq[35][19] = 462;
                EucTwFreq[44][47] = 461;
                EucTwFreq[35][15] = 460;
                EucTwFreq[82][59] = 459;
                EucTwFreq[35][43] = 458;
                EucTwFreq[73][0] = 457;
                EucTwFreq[57][83] = 456;
                EucTwFreq[42][46] = 455;
                EucTwFreq[36][0] = 454;
                EucTwFreq[70][88] = 453;
                EucTwFreq[42][22] = 452;
                EucTwFreq[46][58] = 451;
                EucTwFreq[36][34] = 450;
                EucTwFreq[39][24] = 449;
                EucTwFreq[35][55] = 448;
                EucTwFreq[44][91] = 447;
                EucTwFreq[37][51] = 446;
                EucTwFreq[36][19] = 445;
                EucTwFreq[69][90] = 444;
                EucTwFreq[55][35] = 443;
                EucTwFreq[35][54] = 442;
                EucTwFreq[49][61] = 441;
                EucTwFreq[36][67] = 440;
                EucTwFreq[88][34] = 439;
                EucTwFreq[35][17] = 438;
                EucTwFreq[65][69] = 437;
                EucTwFreq[74][89] = 436;
                EucTwFreq[37][31] = 435;
                EucTwFreq[43][48] = 434;
                EucTwFreq[89][27] = 433;
                EucTwFreq[42][79] = 432;
                EucTwFreq[69][57] = 431;
                EucTwFreq[36][13] = 430;
                EucTwFreq[35][62] = 429;
                EucTwFreq[65][47] = 428;
                EucTwFreq[56][8] = 427;
                EucTwFreq[38][79] = 426;
                EucTwFreq[37][64] = 425;
                EucTwFreq[64][64] = 424;
                EucTwFreq[38][53] = 423;
                EucTwFreq[38][31] = 422;
                EucTwFreq[56][81] = 421;
                EucTwFreq[36][22] = 420;
                EucTwFreq[43][4] = 419;
                EucTwFreq[36][90] = 418;
                EucTwFreq[38][62] = 417;
                EucTwFreq[66][85] = 416;
                EucTwFreq[39][1] = 415;
                EucTwFreq[59][40] = 414;
                EucTwFreq[58][93] = 413;
                EucTwFreq[44][43] = 412;
                EucTwFreq[39][49] = 411;
                EucTwFreq[64][2] = 410;
                EucTwFreq[41][35] = 409;
                EucTwFreq[60][22] = 408;
                EucTwFreq[35][91] = 407;
                EucTwFreq[78][1] = 406;
                EucTwFreq[36][14] = 405;
                EucTwFreq[82][29] = 404;
                EucTwFreq[52][86] = 403;
                EucTwFreq[40][16] = 402;
                EucTwFreq[91][52] = 401;
                EucTwFreq[50][75] = 400;
                EucTwFreq[64][30] = 399;
                EucTwFreq[90][78] = 398;
                EucTwFreq[36][52] = 397;
                EucTwFreq[55][87] = 396;
                EucTwFreq[57][5] = 395;
                EucTwFreq[57][31] = 394;
                EucTwFreq[42][35] = 393;
                EucTwFreq[69][50] = 392;
                EucTwFreq[45][8] = 391;
                EucTwFreq[50][87] = 390;
                EucTwFreq[69][55] = 389;
                EucTwFreq[92][3] = 388;
                EucTwFreq[36][43] = 387;
                EucTwFreq[64][10] = 386;
                EucTwFreq[56][25] = 385;
                EucTwFreq[60][68] = 384;
                EucTwFreq[51][46] = 383;
                EucTwFreq[50][0] = 382;
                EucTwFreq[38][30] = 381;
                EucTwFreq[50][85] = 380;
                EucTwFreq[60][54] = 379;
                EucTwFreq[73][6] = 378;
                EucTwFreq[73][28] = 377;
                EucTwFreq[56][19] = 376;
                EucTwFreq[62][69] = 375;
                EucTwFreq[81][66] = 374;
                EucTwFreq[40][32] = 373;
                EucTwFreq[76][31] = 372;
                EucTwFreq[35][10] = 371;
                EucTwFreq[41][37] = 370;
                EucTwFreq[52][82] = 369;
                EucTwFreq[91][72] = 368;
                EucTwFreq[37][29] = 367;
                EucTwFreq[56][30] = 366;
                EucTwFreq[37][80] = 365;
                EucTwFreq[81][56] = 364;
                EucTwFreq[70][3] = 363;
                EucTwFreq[76][15] = 362;
                EucTwFreq[46][47] = 361;
                EucTwFreq[35][88] = 360;
                EucTwFreq[61][58] = 359;
                EucTwFreq[37][37] = 358;
                EucTwFreq[57][22] = 357;
                EucTwFreq[41][23] = 356;
                EucTwFreq[90][66] = 355;
                EucTwFreq[39][60] = 354;
                EucTwFreq[38][0] = 353;
                EucTwFreq[37][87] = 352;
                EucTwFreq[46][2] = 351;
                EucTwFreq[38][56] = 350;
                EucTwFreq[58][11] = 349;
                EucTwFreq[48][10] = 348;
                EucTwFreq[74][4] = 347;
                EucTwFreq[40][42] = 346;
                EucTwFreq[41][52] = 345;
                EucTwFreq[61][92] = 344;
                EucTwFreq[39][50] = 343;
                EucTwFreq[47][88] = 342;
                EucTwFreq[88][36] = 341;
                EucTwFreq[45][73] = 340;
                EucTwFreq[82][3] = 339;
                EucTwFreq[61][36] = 338;
                EucTwFreq[60][33] = 337;
                EucTwFreq[38][27] = 336;
                EucTwFreq[35][83] = 335;
                EucTwFreq[65][24] = 334;
                EucTwFreq[73][10] = 333;
                EucTwFreq[41][13] = 332;
                EucTwFreq[50][27] = 331;
                EucTwFreq[59][50] = 330;
                EucTwFreq[42][45] = 329;
                EucTwFreq[55][19] = 328;
                EucTwFreq[36][77] = 327;
                EucTwFreq[69][31] = 326;
                EucTwFreq[60][7] = 325;
                EucTwFreq[40][88] = 324;
                EucTwFreq[57][56] = 323;
                EucTwFreq[50][50] = 322;
                EucTwFreq[42][37] = 321;
                EucTwFreq[38][82] = 320;
                EucTwFreq[52][25] = 319;
                EucTwFreq[42][67] = 318;
                EucTwFreq[48][40] = 317;
                EucTwFreq[45][81] = 316;
                EucTwFreq[57][14] = 315;
                EucTwFreq[42][13] = 314;
                EucTwFreq[78][0] = 313;
                EucTwFreq[35][51] = 312;
                EucTwFreq[41][67] = 311;
                EucTwFreq[64][23] = 310;
                EucTwFreq[36][65] = 309;
                EucTwFreq[48][50] = 308;
                EucTwFreq[46][69] = 307;
                EucTwFreq[47][89] = 306;
                EucTwFreq[41][48] = 305;
                EucTwFreq[60][56] = 304;
                EucTwFreq[44][82] = 303;
                EucTwFreq[47][35] = 302;
                EucTwFreq[49][3] = 301;
                EucTwFreq[49][69] = 300;
                EucTwFreq[45][93] = 299;
                EucTwFreq[60][34] = 298;
                EucTwFreq[60][82] = 297;
                EucTwFreq[61][61] = 296;
                EucTwFreq[86][42] = 295;
                EucTwFreq[89][60] = 294;
                EucTwFreq[48][31] = 293;
                EucTwFreq[35][75] = 292;
                EucTwFreq[91][39] = 291;
                EucTwFreq[53][19] = 290;
                EucTwFreq[39][72] = 289;
                EucTwFreq[69][59] = 288;
                EucTwFreq[41][7] = 287;
                EucTwFreq[54][13] = 286;
                EucTwFreq[43][28] = 285;
                EucTwFreq[36][6] = 284;
                EucTwFreq[45][75] = 283;
                EucTwFreq[36][61] = 282;
                EucTwFreq[38][21] = 281;
                EucTwFreq[45][14] = 280;
                EucTwFreq[61][43] = 279;
                EucTwFreq[36][63] = 278;
                EucTwFreq[43][30] = 277;
                EucTwFreq[46][51] = 276;
                EucTwFreq[68][87] = 275;
                EucTwFreq[39][26] = 274;
                EucTwFreq[46][76] = 273;
                EucTwFreq[36][15] = 272;
                EucTwFreq[35][40] = 271;
                EucTwFreq[79][60] = 270;
                EucTwFreq[46][7] = 269;
                EucTwFreq[65][72] = 268;
                EucTwFreq[69][88] = 267;
                EucTwFreq[47][18] = 266;
                EucTwFreq[37][0] = 265;
                EucTwFreq[37][49] = 264;
                EucTwFreq[67][37] = 263;
                EucTwFreq[36][91] = 262;
                EucTwFreq[75][48] = 261;
                EucTwFreq[75][63] = 260;
                EucTwFreq[83][87] = 259;
                EucTwFreq[37][44] = 258;
                EucTwFreq[73][54] = 257;
                EucTwFreq[51][61] = 256;
                EucTwFreq[46][57] = 255;
                EucTwFreq[55][21] = 254;
                EucTwFreq[39][66] = 253;
                EucTwFreq[47][11] = 252;
                EucTwFreq[52][8] = 251;
                EucTwFreq[82][81] = 250;
                EucTwFreq[36][57] = 249;
                EucTwFreq[38][54] = 248;
                EucTwFreq[43][81] = 247;
                EucTwFreq[37][42] = 246;
                EucTwFreq[40][18] = 245;
                EucTwFreq[80][90] = 244;
                EucTwFreq[37][84] = 243;
                EucTwFreq[57][15] = 242;
                EucTwFreq[38][87] = 241;
                EucTwFreq[37][32] = 240;
                EucTwFreq[53][53] = 239;
                EucTwFreq[89][29] = 238;
                EucTwFreq[81][53] = 237;
                EucTwFreq[75][3] = 236;
                EucTwFreq[83][73] = 235;
                EucTwFreq[66][13] = 234;
                EucTwFreq[48][7] = 233;
                EucTwFreq[46][35] = 232;
                EucTwFreq[35][86] = 231;
                EucTwFreq[37][20] = 230;
                EucTwFreq[46][80] = 229;
                EucTwFreq[38][24] = 228;
                EucTwFreq[41][68] = 227;
                EucTwFreq[42][21] = 226;
                EucTwFreq[43][32] = 225;
                EucTwFreq[38][20] = 224;
                EucTwFreq[37][59] = 223;
                EucTwFreq[41][77] = 222;
                EucTwFreq[59][57] = 221;
                EucTwFreq[68][59] = 220;
                EucTwFreq[39][43] = 219;
                EucTwFreq[54][39] = 218;
                EucTwFreq[48][28] = 217;
                EucTwFreq[54][28] = 216;
                EucTwFreq[41][44] = 215;
                EucTwFreq[51][64] = 214;
                EucTwFreq[47][72] = 213;
                EucTwFreq[62][67] = 212;
                EucTwFreq[42][43] = 211;
                EucTwFreq[61][38] = 210;
                EucTwFreq[76][25] = 209;
                EucTwFreq[48][91] = 208;
                EucTwFreq[36][36] = 207;
                EucTwFreq[80][32] = 206;
                EucTwFreq[81][40] = 205;
                EucTwFreq[37][5] = 204;
                EucTwFreq[74][69] = 203;
                EucTwFreq[36][82] = 202;
                EucTwFreq[46][59] = 201;

                #endregion
            }
        }

        #endregion

        #region ToByteArray.....

        /// <summary>
        /// 将此实例中的指定 <see cref="sbyte"/> 字符数组转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="sbyteArray">要转换的 <see cref="sbyte"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            byte[] byteArray = new byte[sbyteArray.Length];
            for (int index = 0; index < sbyteArray.Length; index++)
                byteArray[index] = (byte)sbyteArray[index];
            return byteArray;
        }

        /// <summary>
        /// 将此实例中的指定字符串转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="sourceString">要转换的字符串</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(string sourceString)
        {
            byte[] byteArray = new byte[sourceString.Length];
            for (int index = 0; index < sourceString.Length; index++)
                byteArray[index] = (byte)sourceString[index];
            return byteArray;
        }

        /// <summary>
        /// 将此实例中的指定 <see cref="object"/> 数组转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="tempObjectArray">要转换的 <see cref="object"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(object[] tempObjectArray)
        {
            byte[] byteArray = new byte[tempObjectArray.Length];
            for (int index = 0; index < tempObjectArray.Length; index++)
                byteArray[index] = (byte)tempObjectArray[index];
            return byteArray;
        }

        #endregion

        #region ToSByteArray.....

        /// <summary>
        /// 将此实例中的指定 <see cref="byte"/> 字符数组转换到 <see cref="sbyte"/> 字符数组。
        /// </summary>
        /// <param name="byteArray">要转换的 <see cref="byte"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="sbyte"/> 字符数组</returns>
        public static sbyte[] ToSByteArray(byte[] byteArray)
        {
            sbyte[] sbyteArray = new sbyte[byteArray.Length];
            for (int index = 0; index < byteArray.Length; index++)
                sbyteArray[index] = (sbyte)byteArray[index];
            return sbyteArray;
        }

        #endregion

        #region ReadInput.....

        /// <summary>从流读取字节序列,并将此流中的位置提升读取的字节数.</summary>
        /// <param name="sourceStream">要读取的流.</param>
        /// <param name="target">字节数组。此方法返回时,该缓冲区包含指定的字符数组,该数组的 start 和 (start + count-1) 之间的值由从当前源中读取的字节替换。</param>
        /// <param name="start">buffer 中的从零开始的字节偏移量,从此处开始存储从当前流中读取的数据。.</param>
        /// <param name="count">要从当前流中最多读取的字节数。</param>
        /// <returns>读入缓冲区中的总字节数。如果当前可用的字节数没有请求的字节数那么多,则总字节数可能小于请求的字节数,或者如果已到达流的末尾,则为零 (0)。</returns>
        /// <exception cref="ArgumentException">start 与 count 的和大于缓冲区长度。</exception>
        /// <exception cref="ArgumentNullException">target 为空引用(Visual Basic 中为 Nothing)。</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset 或 count 为负。</exception>
        /// <exception cref="System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="NotSupportedException">流不支持读取。</exception>
        /// <exception cref="ObjectDisposedException">在流关闭后调用方法。</exception>
        public static int ReadInput(System.IO.Stream sourceStream, ref sbyte[] target, int start, int count)
        {
            // Returns 0 bytes if not enough space in target
            if (target.Length == 0)
                return 0;

            byte[] receiver = new byte[target.Length];
            int bytesRead = sourceStream.Read(receiver, start, count);

            // Returns -1 if EOF
            if (bytesRead == 0)
                return -1;

            for (int i = start; i < start + bytesRead; i++)
                target[i] = (sbyte)receiver[i];

            return bytesRead;
        }

        /// <summary>从字符系列读取字节序列,并将此字符系列中的位置提升读取的字节数。</summary>
        /// <param name="sourceTextReader">要读取的流。</param>
        /// <param name="target">字节数组。此方法返回时,该缓冲区包含指定的字符数组,该数组的 start 和 (start + count-1) 之间的值由从当前源中读取的字节替换。</param>
        /// <param name="start">buffer 中的从零开始的字节偏移量,从此处开始存储从当前流中读取的数据。.</param>
        /// <param name="count">要从当前流中最多读取的字节数。</param>
        /// <returns>读入缓冲区中的总字节数。如果当前可用的字节数没有请求的字节数那么多,则总字节数可能小于请求的字节数,或者如果已到达流的末尾,则为零 (0)。</returns>
        /// <exception cref="ArgumentException">start 与 count 的和大于缓冲区长度。</exception>
        /// <exception cref="ArgumentNullException">target 为空引用(Visual Basic 中为 Nothing)。</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset 或 count 为负。</exception>
        /// <exception cref="System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="NotSupportedException">流不支持读取。</exception>
        /// <exception cref="ObjectDisposedException">在流关闭后调用方法。</exception>
        public static int ReadInput(System.IO.TextReader sourceTextReader, ref sbyte[] target, int start, int count)
        {
            // Returns 0 bytes if not enough space in target
            if (target.Length == 0) return 0;

            char[] charArray = new char[target.Length];
            int bytesRead = sourceTextReader.Read(charArray, start, count);

            // Returns -1 if EOF
            if (bytesRead == 0) return -1;

            for (int index = start; index < start + bytesRead; index++)
                target[index] = (sbyte)charArray[index];

            return bytesRead;
        }

        #endregion

        #region FileLength.....

        /// <summary>
        /// 检测当前文件的大小
        /// </summary>
        /// <returns>当前文件的大小。</returns>
        /// <summary>
        /// This method returns the literal value received
        /// </summary>
        /// <param name="literal">The literal to return</param>
        /// <returns>The received value</returns>
        public static long Identity(long literal)
        {
            return literal;
        }

        /// <summary>
        /// This method returns the literal value received
        /// </summary>
        /// <param name="literal">The literal to return</param>
        /// <returns>The received value</returns>
        public static ulong Identity(ulong literal)
        {
            return literal;
        }

        /// <summary>
        /// This method returns the literal value received
        /// </summary>
        /// <param name="literal">The literal to return</param>
        /// <returns>The received value</returns>
        public static float Identity(float literal)
        {
            return literal;
        }

        /// <summary>
        /// This method returns the literal value received
        /// </summary>
        /// <param name="literal">The literal to return</param>
        /// <returns>The received value</returns>
        public static double Identity(double literal)
        {
            return literal;
        }

        #endregion

        #endregion
    }

    #endregion
}

