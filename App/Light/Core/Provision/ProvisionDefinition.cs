using System.Collections.Generic;
using Light.Core.Provision.Tasks;

namespace Light.Core.Provision
{
    public class ProvisionDefinition : List<IProvisionTask>
    {
        public ProvisionDefinition()
        {
            Add(new SettingsProvisionTask());
            Add(new BackgroundProvisionTask());
            Add(new InitialStateProvisionTask());
            Add(new JumplistProvisionTask());
            Add(new MetadataSettingsProvisionTask());
        }
    }
}
