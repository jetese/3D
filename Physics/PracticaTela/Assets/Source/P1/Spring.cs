using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring  {

    #region InEditorVariables

    public float Stiffness;
    Node nodeA;
    Node nodeB;

    #endregion

    public float Length0;
    public float Length;

    public MassSpringCloth Manager;

    public  Spring(Node a, Node b, float dureza)
    {
        nodeA = a;
        nodeB = b;
        Length0 =  Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
        Stiffness = dureza;
    }

    public void ActualizarLongitud()
    {
        Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
    }

    public void AddForces(float beta)
    {
        Length = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).magnitude;
        Vector3 ForceA = -Stiffness * (Length - Length0) * (nodeA.ObtenerPos() - nodeB.ObtenerPos()) / Length;
        Vector3 vectorVelocidad = nodeA.Vel - nodeB.Vel;
        Vector3 vectorDirector = (nodeA.ObtenerPos() - nodeB.ObtenerPos()).normalized;
        Vector3 ForceB =  -beta * Stiffness * Vector3.Dot(vectorVelocidad , vectorDirector) * vectorDirector;
        nodeA.AddForce(ForceA+ForceB);
        nodeB.AddForce(-ForceA-ForceB);
    }
}
