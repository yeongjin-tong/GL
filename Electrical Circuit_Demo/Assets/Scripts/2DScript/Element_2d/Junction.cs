using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Video;

public class Junction : ElectricalComponent
{
    private ConnectionPoint myPoint;

    private void Awake()
    {
        myPoint = GetComponent<ConnectionPoint>();
    }

    ///<summary>
      /// 연결된 전선이 삭제될 때 호출되어,
      /// 이 Junction이 계속 필요한 지 확인하고 스스로를 정리합니다.
    /// </summary>
    public void CheckAndHeal()
    {
        Wire[] allWires = FindObjectsOfType<Wire>();

        List<Wire> connectedWires = allWires
            .Where(w => w.connectedPoints.Contains(myPoint))
            .ToList();

        if (connectedWires.Count == 2)
        {
            Wire wireA = connectedWires[0];
            Wire wireB = connectedWires[1];

            ConnectionPoint pointA = wireA.connectedPoints.First(p => p != myPoint);
            ConnectionPoint pointB = wireB.connectedPoints.First(p => p != myPoint);

            List<Vector3> pathA = GetWirePath(wireA);
            List<Vector3> pathB = GetWirePath(wireB);

                   // pathA : Last, pathB : First가 Junction이어야 함
            if(pathA.First() == pathB.First())
            {
                pathA.Reverse();
            }
            else if (pathA.First() == pathB.Last())
            {
                pathA.Reverse();
                pathB.Reverse();
            }
            else if (pathA.Last() == pathB.Last())
            {
                pathB.Reverse();
            }

            pathA = pathA.Distinct().ToList();
            pathB = pathB.Distinct().ToList();

            pathA.AddRange(pathB.Skip(1));

            WireManager.Instance.CreateWireWithPath(pointA, pointB, pathA);

            Destroy(wireA.gameObject);
            Destroy(wireB.gameObject);
            Destroy(this.gameObject);
        }

        else if(connectedWires.Count <= 1)
        {
            foreach (var wire in connectedWires)
            {
                Destroy(wire.gameObject);
            }
            Destroy(this.gameObject);
        }
    }

    private List<Vector3> GetWirePath(Wire wire)
    {
        var lr = wire.GetComponent<LineRenderer>();
        var path = new Vector3[lr.positionCount];
        lr.GetPositions(path);
        return path.ToList();
    }
}
