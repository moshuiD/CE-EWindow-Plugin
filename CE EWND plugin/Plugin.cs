using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CESDK;
namespace CE_EWND_plugin
{
    public class Plugin : CESDKPluginClass
    {
        public override bool DisablePlugin()
        {
            return true;
        }

        public override bool EnablePlugin()
        {
            sdk.lua.Register("EnableFuc",Main);
            sdk.lua.DoString(@"local m=MainForm.Menu
                               local topm=createMenuItem(m)
                               topm.Caption='EWNDPlugin'
                               m.Items.insert(MainForm.miHelp.MenuIndex,topm)

                               local mf=createMenuItem(m)
                               mf.Caption='OpenWindows';
                               mf.OnClick=function(s)
                               local i=EnableFuc(newthread)
                               end
                               topm.add(mf)");
            return true;
        }

        public override string GetPluginName()
        {
            return"CE EWND Pulgin made by \"moshui\"";
        }
        int Main()
        {
            MainForm form = new MainForm();
            Application.Run(form);
            return 1;
        }
    }
}
