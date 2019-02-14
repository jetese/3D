using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
public class MassSpringCloth : MonoBehaviour 
{
    Mesh malla;
    Vector3[] vertices;
    int[] triangulos;

    Dictionary<Edge,int> opuesto = new Dictionary<Edge, int>();
    List<NodeTela> nodes = new List<NodeTela>();
    List<SpringTela> springs = new List<SpringTela>();
    Edge auxEdge;

    struct Edge
    {
        public int verticeA;
        public int verticeB;
        public Edge(int a, int b)
        {
            if (a > b)
            {
                verticeA = a;
                verticeB = b;
            }
            else
            {
                verticeA = b;
                verticeB = a;
            }
        }
    }

    /// <summary>
    /// Default constructor. Zero all. 
    /// </summary>
    public MassSpringCloth()
	{
		this.Paused = true;
		this.TimeStep = 0.01f;
		this.Gravity = new Vector3 (0.0f, -9.81f, 0.0f);
		this.IntegrationMethod = Integration.Explicit;

        
	}

	/// <summary>
	/// Integration method.
	/// </summary>
	public enum Integration
	{
		Explicit = 0,
		Symplectic = 1,
	};

	#region InEditorVariables

	public bool Paused;
	public float TimeStep;
    public Vector3 Gravity;
	public Integration IntegrationMethod;
    public float Mass;
    public float StiffnessTraccion;
    public float StiffnessFlexion;
    public float alpha;
    public float beta;
    
    public bool ModoDebug = false;
    public Collider[] SelectVertex;
    public GameObject cylinder;
    public Vector3 viento;
    public float fuerzaViento;
    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        malla = GetComponent<MeshFilter>().mesh;
        vertices = malla.vertices;
        triangulos = malla.triangles;
        for (int i = 0; i < vertices.Length; i++)
        {
            bool fijado = false;
            Vector3 posicion = this.transform.TransformPoint(vertices[i]);
            for (int j = 0; j < SelectVertex.Length; j++)
            {
                if (SelectVertex[j].bounds.Contains(posicion))
                {
                    fijado = true;
                }
            }
            nodes.Add(new NodeTela(Mass, posicion, fijado));
        }

        int[] indices =
        {
            0, 1, 2, 0, 1
        };

        
        for (int i = 0; i < triangulos.Length; i = i + 3)
        {
            for (int j = 0; j < 3; j++)
            {
                auxEdge = new Edge(triangulos[i + indices[j]], triangulos[i + indices[j + 1]]);
                if (!opuesto.ContainsKey(auxEdge))
                {
                    springs.Add(new SpringTela(nodes[triangulos[i + indices[j]]], nodes[triangulos[i + indices[j + 1]]], StiffnessTraccion));
                    opuesto.Add(auxEdge, triangulos[i + indices[j + 2]]);

                    if(ModoDebug)
                        Instantiate(cylinder, 0.5f * (nodes[triangulos[i + indices[j]]].Pos + nodes[triangulos[i + indices[j + 1]]].Pos), Quaternion.FromToRotation(new Vector3(0.0f, 1.0f, 0.0f), nodes[triangulos[i + indices[j]]].Pos - nodes[triangulos[i + indices[j + 1]]].Pos));
                }
                else
                {
                    springs.Add(new SpringTela(nodes[opuesto[auxEdge]], nodes[triangulos[i + indices[j + 2]]], StiffnessFlexion));

                    if (ModoDebug)
                        Instantiate(cylinder, 0.5f * (nodes[opuesto[auxEdge]].Pos + nodes[triangulos[i + indices[j + 2]]].Pos), Quaternion.FromToRotation(new Vector3(0.0f, 1.0f, 0.0f), nodes[opuesto[auxEdge]].Pos - nodes[triangulos[i + indices[j + 2]]].Pos));
                }
            }
        }

        foreach (NodeTela node in nodes)
        {
            node.Manager = this;
        }
        foreach(SpringTela spring in springs)
        {
            spring.Manager = this;
        }
    }

	public void Update()
	{
		if (Input.GetKeyUp (KeyCode.P))
			this.Paused = !this.Paused;
        ActualizarNodos();
    }

    public void FixedUpdate()
    {
        if (this.Paused)
            return; // Not simulating

        // Select integration method
        switch (this.IntegrationMethod)
        {
            case Integration.Explicit: this.stepExplicit(); break;
            case Integration.Symplectic: this.stepSymplectic(); break;
            default:
                throw new System.Exception("[ERROR] Should never happen!");
        }
        
    }

    #endregion

    /// <summary>
    /// Performs a simulation step in 1D using Explicit integration.
    /// </summary>
    private void stepExplicit()
	{
        foreach (NodeTela node in nodes)
        {
            node.Force = Vector3.zero;
        }
        foreach (NodeTela node in nodes)
        {
            node.AddForces(alpha);
        }

        foreach (SpringTela spring in springs)
        {
            spring.AddForces(alpha);
        }
        foreach (NodeTela node in nodes)
        {
            if (!node.Fixed)
            {
                node.Pos += TimeStep * node.Vel;
                node.Vel += TimeStep * node.Force / node.Mass;
            }
        }
	}

	/// <summary>
	/// Performs a simulation step in 1D using Symplectic integration.
	/// </summary>
	private void stepSymplectic()
	{
        foreach (NodeTela node in nodes)
        {
            node.Force = Vector3.zero;
        }
        foreach (NodeTela node in nodes)
        {
            node.AddForces(beta);
        }

        for (int i = 0; i < malla.triangles.Length; i += 3)
        {
            Vector3 FuerzaVientoNodo = CalcularViento(nodes[malla.triangles[i]], nodes[malla.triangles[i + 1]], nodes[malla.triangles[i + 2]], viento, fuerzaViento);
            nodes[malla.triangles[i]].AddForce(FuerzaVientoNodo);
            nodes[malla.triangles[i + 1]].AddForce(FuerzaVientoNodo);
            nodes[malla.triangles[i + 2]].AddForce(FuerzaVientoNodo);
        }

        foreach (SpringTela spring in springs)
        {
            spring.AddForces(beta);
        }


        foreach (NodeTela node in nodes)
        {
            if (!node.Fixed)
            {
                node.Vel += TimeStep * node.Force / node.Mass;
                node.Pos += TimeStep * node.Vel;               
            }
        }
    }

    private void ActualizarNodos()
    {

        for(int i=0; i< nodes.Count; i++) {
            vertices[i] = this.transform.InverseTransformPoint(nodes[i].ObtenerPos());   
        }
        malla.vertices = vertices;
        for(int i = 0; i < springs.Count; i++)
        {
            springs[i].ActualizarLongitud();
        }
        malla.RecalculateNormals();
    }

    private Vector3 CalcularViento (NodeTela a, NodeTela b, NodeTela c,  Vector3 viento, float k)
    {
        Vector3 cross = Vector3.Cross((b.Pos - a.Pos), (c.Pos - a.Pos));
        Vector3 normalTriangulo = cross.normalized;
        float areaTriangulo = cross.magnitude / 2;
        return (Vector3.Dot(normalTriangulo, viento) * k * areaTriangulo / 3) * normalTriangulo;
    }
}
