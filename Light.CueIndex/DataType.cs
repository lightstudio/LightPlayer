using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.CueIndex
{
    /// <summary>
    /// <list>
    /// <item>AUDIO - Audio/Music (2352)</item>
    /// <item>CDG - Karaoke CD+G (2448)</item>
    /// <item>MODE1/2048 - CDROM Mode1 Data (cooked)</item>
    /// <item>MODE1/2352 - CDROM Mode1 Data (raw)</item>
    /// <item>MODE2/2336 - CDROM-XA Mode2 Data</item>
    /// <item>MODE2/2352 - CDROM-XA Mode2 Data</item>
    /// <item>CDI/2336 - CDI Mode2 Data</item>
    /// <item>CDI/2352 - CDI Mode2 Data</item>
    /// </list>
    /// </summary>
    enum DataType
    {
        AUDIO, CDG, MODE1_2048, MODE1_2352, MODE2_2336, MODE2_2352, CDI_2336, CDI_2352
    }
}
