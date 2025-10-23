using Xinyee;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaterFloat : MonoBehaviour
{
    //public properties
    public float AirDrag = 1;
    public float WaterDrag = 10;
    public bool AffectDirection = true;
    public bool AttachToSurface = false;
    public Transform[] FloatPoints;

    //used components
    protected Rigidbody Rigidbody;
    protected Waves Waves;

    //water line
    protected float WaterLine;
    protected Vector3[] WaterLinePoints;

    //help Vectors
    protected Vector3 smoothVectorRotation;
    protected Vector3 TargetUp;
    protected Vector3 centerOffset;

    public Vector3 Center { get { return transform.position + centerOffset; } }

    // Start is called before the first frame update
    void Awake()
    {
        //get components
        Waves = FindObjectOfType<Waves>();
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.useGravity = false;

        //compute center
        WaterLinePoints = new Vector3[FloatPoints.Length];
        for (int i = 0; i < FloatPoints.Length; i++)
            WaterLinePoints[i] = FloatPoints[i].position;
        centerOffset = PhysicsHelper.GetCenter(WaterLinePoints) - transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //default water surface
        var newWaterLine = 0f;
        var pointUnderWater = false;

        //set WaterLinePoints and WaterLine
        for (int i = 0; i < FloatPoints.Length; i++)
        {
            //height
            WaterLinePoints[i] = FloatPoints[i].position;
            WaterLinePoints[i].y = Waves.GetHeight(FloatPoints[i].position);
            newWaterLine += WaterLinePoints[i].y / FloatPoints.Length;
            if (WaterLinePoints[i].y > FloatPoints[i].position.y)
                pointUnderWater = true;
        }

        var waterLineDelta = newWaterLine - WaterLine;
        WaterLine = newWaterLine;

        //compute up vector
        TargetUp = PhysicsHelper.GetNormal(WaterLinePoints);

        //gravity
        var gravity = Physics.gravity;
        Rigidbody.drag = AirDrag;
        if (WaterLine > Center.y)
        {
            Rigidbody.drag = WaterDrag;
            //under water
            if (AttachToSurface)
            {
                //attach to water surface
                Rigidbody.position = new Vector3(Rigidbody.position.x, WaterLine - centerOffset.y, Rigidbody.position.z);
            }
            else
            {
                //go up
                gravity = AffectDirection ? TargetUp * -Physics.gravity.y : -Physics.gravity;
                transform.Translate(Vector3.up * waterLineDelta * 0.9f);
            }
        }
        Rigidbody.AddForce(gravity * Mathf.Clamp(Mathf.Abs(WaterLine - Center.y), 0, 1));

        //rotation
        if (pointUnderWater)
        {
            //attach to water surface
            TargetUp = Vector3.SmoothDamp(transform.up, TargetUp, ref smoothVectorRotation, 0.2f);
            Rigidbody.rotation = Quaternion.FromToRotation(transform.up, TargetUp) * Rigidbody.rotation;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (FloatPoints == null)
            return;

        // 获取 Waves 引用
        if (Waves == null)
        {
            Waves = FindObjectOfType<Waves>();
        }

        // 计算平均水位线
        float averageWaterLine = 0f;
        int validPoints = 0;

        for (int i = 0; i < FloatPoints.Length; i++)
        {
            if (FloatPoints[i] == null)
                continue;

            // 绘制绿色球体 - 显示浮点当前位置
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(FloatPoints[i].position, 0.1f);

            // 如果 Waves 可用，计算并绘制实际水位
            if (Waves != null)
            {
                try
                {
                    // 在编辑模式下也要实时计算水位高度
                    float waterHeight = Waves.GetHeight(FloatPoints[i].position);
                    Vector3 waterPoint = new Vector3(FloatPoints[i].position.x, waterHeight, FloatPoints[i].position.z);

                    averageWaterLine += waterHeight;
                    validPoints++;

                    // 绘制蓝色立方体 - 显示实际水位位置
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(waterPoint, Vector3.one * 0.2f);

                    // 绘制从浮点到水位的连线
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(FloatPoints[i].position, waterPoint);
                }
                catch (System.Exception)
                {
                    // 如果计算水位失败，跳过这个点
                    continue;
                }
            }
        }

        // 绘制中心点和平均水位线
        if (validPoints > 0 && Waves != null)
        {
            averageWaterLine /= validPoints;

            // 重新计算中心点（用于Gizmo显示）
            Vector3 center = Vector3.zero;
            int centerPoints = 0;
            for (int i = 0; i < FloatPoints.Length; i++)
            {
                if (FloatPoints[i] != null)
                {
                    center += FloatPoints[i].position;
                    centerPoints++;
                }
            }
            if (centerPoints > 0) center /= centerPoints;

            // 绘制红色立方体 - 显示平均水位位置
            Gizmos.color = Color.red;
            Vector3 waterLineCenter = new Vector3(center.x, averageWaterLine, center.z);
            Gizmos.DrawCube(waterLineCenter, Vector3.one * 0.5f);

            // 绘制水位线方向
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(waterLineCenter, TargetUp * 2f);
            }

            // 绘制水位平面（可视化）
            DrawWaterPlaneGizmo(center, averageWaterLine, 3f);
        }
    }

    // 辅助方法：绘制水位平面
    private void DrawWaterPlaneGizmo(Vector3 center, float waterHeight, float size)
    {
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);

        Vector3 corner1 = new Vector3(center.x - size, waterHeight, center.z - size);
        Vector3 corner2 = new Vector3(center.x + size, waterHeight, center.z - size);
        Vector3 corner3 = new Vector3(center.x + size, waterHeight, center.z + size);
        Vector3 corner4 = new Vector3(center.x - size, waterHeight, center.z + size);

        // 绘制四边形边界
        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);

        // 绘制交叉线
        Gizmos.DrawLine(corner1, corner3);
        Gizmos.DrawLine(corner2, corner4);
    }
}