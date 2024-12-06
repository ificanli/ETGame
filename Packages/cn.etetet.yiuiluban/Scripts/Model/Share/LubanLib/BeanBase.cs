using ET;

namespace Luban
{
    [EnableClass]
    public abstract class BeanBase : ITypeId
    {
        public abstract int GetTypeId();

        public virtual void EndInit()
        {
        }
    }
}