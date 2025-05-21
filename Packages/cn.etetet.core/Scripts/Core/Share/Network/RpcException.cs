using System;

namespace ET
{
    /// <summary>
    /// RPC异常,带ErrorCode
    /// </summary>
    public class RpcException: Exception
    {
        public int Error
        {
            get;
        }

        public RpcException(int error, string message): base(message)
        {
            this.Error = error;
        }

        public override string ToString()
        {
            int error = this.Error;
            if (this.Error > ErrorCore.ERR_WithException)
            {
                error -= ErrorCore.ERR_WithException;
            }
            return $"Error: {this.Error} package: {error / 1000} id: {error % 1000}\n{base.ToString()}";
        }
    }
}