using Scriban.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.XLib.Render
{
    public partial class ScribanT
    {
        private string template { get; set; }

        private Dictionary<string, object> globalVariables { get; set; }
        private Scriban.Template xTemplate = null;
        
        private readonly ILogger _logger = Log.ForContext<ScribanT>();

        public ScribanT(string template, Dictionary<string, object> globalVar = null)
        {
            this.template = template;
            globalVariables = globalVar ?? new Dictionary<string, object>();

            xTemplate = Scriban.Template.Parse(template);
            if (xTemplate.HasErrors)
            {
                throw new ApplicationException(xTemplate
                    .Messages
                    .Select(m => m.ToString())
                    .Aggregate((f, s) => f + "\n" + s)
                    );
            }
        }

        public List<string> GetTemplateErrors()
        {
            if (xTemplate.HasErrors)
            {
                return xTemplate
                    .Messages
                    .Select(m => m.ToString())
                    .ToList();
            }
            return new List<string>();
        }

        public async Task<string> RenderAsync(dynamic dmodel)
        {
            try
            {

                var xmodel = new ScriptObject();
                xmodel.Import(new { model = dmodel }, renamer: member => member.Name);
                var xfn = new ScriptObject(); ;
                xfn.Import(typeof(Functions)
                    , renamer: member => member.Name);
                var context = new Scriban.TemplateContext { MemberRenamer = member => member.Name, LoopLimit = 65000 };
                context.PushCulture(System.Globalization.CultureInfo.CurrentCulture);
                context.PushGlobal(xmodel);
                context.PushGlobal(xfn);
                var scriptObjGlobalVariables = new ScriptObject();

                foreach (var kv in globalVariables)
                    scriptObjGlobalVariables[kv.Key] = kv.Value;

                context.PushGlobal(scriptObjGlobalVariables);

                var res = await xTemplate.RenderAsync(context);
                return res;
            }
            catch (Exception e)
            {
                _logger.Error(e, $"ScribanT render error\nTemplate {template}\n\n"
                   + Newtonsoft.Json.JsonConvert.SerializeObject(dmodel));
                throw;
            }

        }

    }
}
