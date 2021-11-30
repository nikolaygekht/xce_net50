using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class KeyboardLayout
    {
        public int LayoutCode { get; }

        public string LayoutName { get; }

        internal KeyboardLayout(string name, int code)
        {
            LayoutName = name;
            LayoutCode = code;
        }
    };

    internal class KeyboardLayouts
    {
        private readonly Dictionary<int, KeyboardLayout> mList = new Dictionary<int, KeyboardLayout>();
        private readonly KeyboardLayout mUnknown = new KeyboardLayout("??", -1);

        public KeyboardLayout this[int index]
        {
            get
            {
                if (mList.TryGetValue(index, out KeyboardLayout l))
                    return l;
                else
                    return mUnknown;
            }
        }

        internal void Add(KeyboardLayout layout)
        {
            mList[layout.LayoutCode] = layout;
        }

        internal KeyboardLayouts()
        {
            Add(new KeyboardLayout("af", 0x0436));
            Add(new KeyboardLayout("is", 0x040F));
            Add(new KeyboardLayout("sq", 0x041C));
            Add(new KeyboardLayout("id", 0x0421));
            Add(new KeyboardLayout("ar-ae", 0x3801));
            Add(new KeyboardLayout("it-it", 0x0410));
            Add(new KeyboardLayout("ar-bh", 0x3C01));
            Add(new KeyboardLayout("it-ch", 0x0810));
            Add(new KeyboardLayout("ar-dz", 0x1401));
            Add(new KeyboardLayout("ja", 0x0411));
            Add(new KeyboardLayout("ar-eg", 0x0C01));
            Add(new KeyboardLayout("ko", 0x0412));
            Add(new KeyboardLayout("ar-iq", 0x0801));
            Add(new KeyboardLayout("lv", 0x0426));
            Add(new KeyboardLayout("ar-jo", 0x2C01));
            Add(new KeyboardLayout("lt", 0x0427));
            Add(new KeyboardLayout("ar-kw", 0x3401));
            Add(new KeyboardLayout("mk", 0x042F));
            Add(new KeyboardLayout("ar-lb", 0x3001));
            Add(new KeyboardLayout("ms-my", 0x043E));
            Add(new KeyboardLayout("ar-ly", 0x1001));
            Add(new KeyboardLayout("ms-bn", 0x083E));
            Add(new KeyboardLayout("ar-ma", 0x1801));
            Add(new KeyboardLayout("mt", 0x043A));
            Add(new KeyboardLayout("ar-om", 0x2001));
            Add(new KeyboardLayout("mr", 0x044E));
            Add(new KeyboardLayout("ar-qa", 0x4001));
            Add(new KeyboardLayout("no-no", 0x0414));
            Add(new KeyboardLayout("ar-sa", 0x0401));
            Add(new KeyboardLayout("no-no", 0x0814));
            Add(new KeyboardLayout("ar-sy", 0x2801));
            Add(new KeyboardLayout("pl", 0x0415));
            Add(new KeyboardLayout("ar-tn", 0x1C01));
            Add(new KeyboardLayout("pt-pt", 0x0816));
            Add(new KeyboardLayout("ar-ye", 0x2401));
            Add(new KeyboardLayout("pt-br", 0x0416));
            Add(new KeyboardLayout("hy", 0x042B));
            Add(new KeyboardLayout("rm", 0x0417));
            Add(new KeyboardLayout("az-az", 0x042C));
            Add(new KeyboardLayout("ro", 0x0418));
            Add(new KeyboardLayout("az-az", 0x082C));
            Add(new KeyboardLayout("ro-mo", 0x0818));
            Add(new KeyboardLayout("eu", 0x042D));
            Add(new KeyboardLayout("ru", 0x0419));
            Add(new KeyboardLayout("be", 0x0423));
            Add(new KeyboardLayout("ru-mo", 0x0819));
            Add(new KeyboardLayout("bg", 0x0402));
            Add(new KeyboardLayout("sa", 0x044F));
            Add(new KeyboardLayout("ca", 0x0403));
            Add(new KeyboardLayout("sr-sp", 0x0C1A));
            Add(new KeyboardLayout("zh-cn", 0x0804));
            Add(new KeyboardLayout("sr-sp", 0x081A));
            Add(new KeyboardLayout("zh-hk", 0x0C04));
            Add(new KeyboardLayout("tn", 0x0432));
            Add(new KeyboardLayout("zh-mo", 0x1404));
            Add(new KeyboardLayout("sl", 0x0424));
            Add(new KeyboardLayout("zh-sg", 0x1004));
            Add(new KeyboardLayout("sk", 0x041B));
            Add(new KeyboardLayout("zh-tw", 0x0404));
            Add(new KeyboardLayout("sb", 0x042E));
            Add(new KeyboardLayout("hr", 0x041A));
            Add(new KeyboardLayout("es-es", 0x0C0A));
            Add(new KeyboardLayout("cs", 0x0405));
            Add(new KeyboardLayout("es-ar", 0x2C0A));
            Add(new KeyboardLayout("da", 0x0406));
            Add(new KeyboardLayout("es-bo", 0x400A));
            Add(new KeyboardLayout("nl-nl", 0x0413));
            Add(new KeyboardLayout("es-cl", 0x340A));
            Add(new KeyboardLayout("nl-be", 0x0813));
            Add(new KeyboardLayout("es-co", 0x240A));
            Add(new KeyboardLayout("en-au", 0x0C09));
            Add(new KeyboardLayout("es-cr", 0x140A));
            Add(new KeyboardLayout("en-bz", 0x2809));
            Add(new KeyboardLayout("es-do", 0x1C0A));
            Add(new KeyboardLayout("en-ca", 0x1009));
            Add(new KeyboardLayout("es-ec", 0x300A));
            Add(new KeyboardLayout("en-cb", 0x2409));
            Add(new KeyboardLayout("es-gt", 0x100A));
            Add(new KeyboardLayout("en-ie", 0x1809));
            Add(new KeyboardLayout("es-hn", 0x480A));
            Add(new KeyboardLayout("en-jm", 0x2009));
            Add(new KeyboardLayout("es-mx", 0x080A));
            Add(new KeyboardLayout("en-nz", 0x1409));
            Add(new KeyboardLayout("es-ni", 0x4C0A));
            Add(new KeyboardLayout("en-ph", 0x3409));
            Add(new KeyboardLayout("es-pa", 0x180A));
            Add(new KeyboardLayout("en-za", 0x1C09));
            Add(new KeyboardLayout("es-pe", 0x280A));
            Add(new KeyboardLayout("en-tt", 0x2C09));
            Add(new KeyboardLayout("es-pr", 0x500A));
            Add(new KeyboardLayout("en-gb", 0x0809));
            Add(new KeyboardLayout("es-py", 0x3C0A));
            Add(new KeyboardLayout("en-us", 0x0409));
            Add(new KeyboardLayout("es-sv", 0x440A));
            Add(new KeyboardLayout("et", 0x0425));
            Add(new KeyboardLayout("es-uy", 0x380A));
            Add(new KeyboardLayout("fa", 0x0429));
            Add(new KeyboardLayout("es-ve", 0x200A));
            Add(new KeyboardLayout("fi", 0x040B));
            Add(new KeyboardLayout("sx", 0x0430));
            Add(new KeyboardLayout("fo", 0x0438));
            Add(new KeyboardLayout("sw", 0x0441));
            Add(new KeyboardLayout("fr-fr", 0x040C));
            Add(new KeyboardLayout("sv-se", 0x041D));
            Add(new KeyboardLayout("fr-be", 0x080C));
            Add(new KeyboardLayout("sv-fi", 0x081D));
            Add(new KeyboardLayout("fr-ca", 0x0C0C));
            Add(new KeyboardLayout("ta", 0x0449));
            Add(new KeyboardLayout("fr-lu", 0x140C));
            Add(new KeyboardLayout("tt", 0X0444));
            Add(new KeyboardLayout("fr-ch", 0x100C));
            Add(new KeyboardLayout("th", 0x041E));
            Add(new KeyboardLayout("gd-ie", 0x083C));
            Add(new KeyboardLayout("tr", 0x041F));
            Add(new KeyboardLayout("gd", 0x043C));
            Add(new KeyboardLayout("ts", 0x0431));
            Add(new KeyboardLayout("de-de", 0x0407));
            Add(new KeyboardLayout("uk", 0x0422));
            Add(new KeyboardLayout("de-at", 0x0C07));
            Add(new KeyboardLayout("ur", 0x0420));
            Add(new KeyboardLayout("de-li", 0x1407));
            Add(new KeyboardLayout("uz-uz", 0x0843));
            Add(new KeyboardLayout("de-lu", 0x1007));
            Add(new KeyboardLayout("uz-uz", 0x0443));
            Add(new KeyboardLayout("de-ch", 0x0807));
            Add(new KeyboardLayout("vi", 0x042A));
            Add(new KeyboardLayout("el", 0x0408));
            Add(new KeyboardLayout("xh", 0x0434));
            Add(new KeyboardLayout("he", 0x040D));
            Add(new KeyboardLayout("yi", 0x043D));
            Add(new KeyboardLayout("hi", 0x0439));
            Add(new KeyboardLayout("zu", 0x0435));
            Add(new KeyboardLayout("hu", 0x040E));
        }
    }
}