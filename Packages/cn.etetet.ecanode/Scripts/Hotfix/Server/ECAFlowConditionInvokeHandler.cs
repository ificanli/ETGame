using ET;

namespace ET.Server
{
    [Invoke]
    public class ECAFlowConditionInvokeHandler : AInvokeHandler<ECAFlowConditionInvoke, bool>
    {
        private const string ParamState = "state";
        private const string ParamPointType = "point_type";

        public override bool Handle(ECAFlowConditionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            switch (args.Key)
            {
                case ECAFlowConditionKey.PlayerInRange:
                    if (point == null || player == null)
                    {
                        return false;
                    }
                    return point.PlayersInRange.Contains(player.Id);
                case ECAFlowConditionKey.PointStateEquals:
                    if (point == null)
                    {
                        return false;
                    }
                    if (!TryGetIntParam(args.Node, ParamState, out int state))
                    {
                        return false;
                    }
                    return point.CurrentState == state;
                case ECAFlowConditionKey.PointTypeEquals:
                    if (point == null)
                    {
                        return false;
                    }
                    if (!TryGetIntParam(args.Node, ParamPointType, out int pointType))
                    {
                        return false;
                    }
                    return point.PointType == pointType;
                default:
                    return true;
            }
        }

        private static bool TryGetIntParam(FlowNodeData node, string key, out int value)
        {
            value = 0;
            if (node == null || node.Params == null || node.Params.Count == 0)
            {
                return false;
            }

            foreach (FlowParam param in node.Params)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                return int.TryParse(param.Value, out value);
            }

            return false;
        }
    }
}
