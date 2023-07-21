using EchoDotNetLite.Models;
using System.Text;

namespace EchoDotNetLite.Common
{
    public static class Extentions
    {
        public static string GetDebugString(this EchoObjectInstance echoObjectInstance)
        {
            if (echoObjectInstance == null)
            {
                return "null";
            }
            if (echoObjectInstance.Spec == null)
            {
                return "Spec null";
            }
            return $"0x{echoObjectInstance.Spec.ClassGroup.ClassGroupCode:X2}{echoObjectInstance.Spec.ClassGroup.ClassGroupName} 0x{echoObjectInstance.Spec.Class.ClassCode:X2}{echoObjectInstance.Spec.Class.ClassName} {echoObjectInstance.InstanceCode:X2}";
        }
        public static string GetDebugString(this EchoPropertyInstance echoPropertyInstance)
        {
            if (echoPropertyInstance == null)
            {
                return "null";
            }
            if (echoPropertyInstance.Spec == null)
            {
                return "Spec null";
            }
            var sb = new StringBuilder();
            sb.Append($"0x{echoPropertyInstance.Spec.Code:X2}");
            sb.Append(echoPropertyInstance.Spec.Name);
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Get ? "Get" : "");
            sb.Append(echoPropertyInstance.Spec.GetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Set ? "Set" : "");
            sb.Append(echoPropertyInstance.Spec.SetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Anno ? "Anno" : "");
            sb.Append(echoPropertyInstance.Spec.AnnoRequired ? "(Req)" : "");
            return sb.ToString();
        }
    }
}
