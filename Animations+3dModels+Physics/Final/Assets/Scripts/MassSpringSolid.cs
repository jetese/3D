using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class MassSpringSolid : MonoBehaviour {
    Mesh malla;
    Vector3[] vertices;

    Dictionary<Edge, Spring> springs = new Dictionary<Edge, Spring>();
    Dictionary<int, int> contenido = new Dictionary<int, int>();
    List<Node> nodes = new List<Node>();
    List<float[]> weight = new List<float[]>();
    List<float> volumenesTetraedos = new List<float>();
    //List<Spring> springs = new List<Spring>();

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
    public MassSpringSolid()
    {
        this.Paused = true;
        this.TimeStep = 0.01f;
        this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
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
    public float StiffnessTraccion;
    public float alpha;
    public float beta;
    public float Density;

    public bool ModoDebug = false;
    public Collider[] SelectVertex;
    //public Vector3 viento;
    //public float fuerzaViento;

    public TextAsset NodesText;
    public TextAsset ElementsText;
    List<Vector4> Tetraedros = new List<Vector4>();
    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        bool fijado;
        //Lectura de archivos y conversion a Nodo
        string[] VertexNodesString = NodesText.text.Replace("\r\n",",").Split(',');

        for(int i = 1; i < VertexNodesString.Length -2; i++)
        {
            //Convertimos las lineas a array de string
            string[] VerticesString = Regex.Replace(VertexNodesString[i], "( )+",",").Split(',');

            //Comprobamos si el vértice está contenido por alguna caja de fijado
            fijado = false;
            Vector3 position = this.transform.TransformPoint(new Vector3(-float.Parse(VerticesString[2]), float.Parse(VerticesString[4]), -float.Parse(VerticesString[3])));
            for (int j = 0; j < SelectVertex.Length; j++)
            {
                if (SelectVertex[j].bounds.Contains(position))
                {
                    fijado = true;
                }
            }

            //Añadimos el nodo y le asignamos el Manager
            nodes.Add(new Node(0, position, fijado));
            nodes[nodes.Count - 1].Manager = this;
        }

        Node[] elementosAux = new Node[4];
        Vector4 tetraedroAux;
        float volumeTotal;
        string[] tetraedrosString = ElementsText.text.Replace("\r\n", ",").Split(',');
        //Sacamos los tetraedros del archivo, creamos la lista de tetraedos y añadimos la masa calculada a partir del volumen de cada tetraedro
        for (int i = 1; i < tetraedrosString.Length - 2; i++)
        {
            string[] tetraedroString = Regex.Replace(tetraedrosString[i], "( )+", ",").Split(',');

            Vector4 tetraedro = new Vector4(int.Parse(tetraedroString[2]) - 1, int.Parse(tetraedroString[3]) - 1, int.Parse(tetraedroString[4]) - 1, int.Parse(tetraedroString[5]) - 1);

            Tetraedros.Add(tetraedro);


            tetraedroAux = Tetraedros[Tetraedros.Count - 1];
            elementosAux[0] = nodes[(int)tetraedro.x];
            elementosAux[1] = nodes[(int)tetraedro.y];
            elementosAux[2] = nodes[(int)tetraedro.z];
            elementosAux[3] = nodes[(int)tetraedro.w];

            volumeTotal = Mathf.Abs(Vector3.Dot(Vector3.Cross(elementosAux[1].Pos - elementosAux[0].Pos, elementosAux[2].Pos - elementosAux[0].Pos), elementosAux[3].Pos - elementosAux[0].Pos) / 6);
            volumenesTetraedos.Add(volumeTotal);
            for (int j = 0; j < elementosAux.Length; j++)
            {
                elementosAux[j].AddMass(volumeTotal * Density / 4);
            }
            //nodes[(int)tetraedro.x].AddMass(volumeTotal * Density / 4);
            //nodes[(int)tetraedro.y].AddMass(volumeTotal * Density / 4);
            //nodes[(int)tetraedro.z].AddMass(volumeTotal * Density / 4);
            //nodes[(int)tetraedro.w].AddMass(volumeTotal * Density / 4);
        }

        int[] indiceNodos = new int[4];
        int contador = 0;
        //Creamos los muelles
        foreach (Vector4 tetra in Tetraedros)
        {
            indiceNodos[0] = (int)tetra.x;
            indiceNodos[1] = (int)tetra.y;
            indiceNodos[2] = (int)tetra.z;
            indiceNodos[3] = (int)tetra.w;

            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    Edge indice = new Edge(indiceNodos[i], indiceNodos[j]);
                    if (!springs.ContainsKey(indice))
                    {
                        Spring muelle = new Spring(nodes[indiceNodos[i]], nodes[indiceNodos[j]], StiffnessTraccion, volumenesTetraedos[contador]/6);
                        muelle.Manager = this;
                        springs.Add(indice, muelle);
                    }
                    else
                    {
                        springs[indice].AddVolume(volumenesTetraedos[contador] / 6);
                    }
                }
            }
            contador++;
        }


        Vector3[] elementos = new Vector3[4];
        malla = GetComponent<MeshFilter>().mesh;
        vertices = malla.vertices;
        contador = 0;

        //Creamos un array de todos los pesos calculados de cada Nodo
        bool estaContenido = false;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 verticeMundo = transform.TransformPoint(vertices[i]);
            float[] pesos = new float[4];


            estaContenido = false;
            while (contador < Tetraedros.Count && !estaContenido)
            {
                Vector4 tetraedro = Tetraedros[contador];
                elementos[0] = nodes[(int)tetraedro.x].Pos;
                elementos[1] = nodes[(int)tetraedro.y].Pos;
                elementos[2] = nodes[(int)tetraedro.z].Pos;
                elementos[3] = nodes[(int)tetraedro.w].Pos;
                if (inVolume(verticeMundo, elementos))
                {
                    contenido[i] = contador;
                    estaContenido = true;
                }
                contador++;
            }
            contador--;
            volumeTotal = Mathf.Abs(Vector3.Dot(Vector3.Cross(elementos[1] - elementos[0], elementos[2] - elementos[0]), elementos[3] - elementos[0]) / 6);
            if (!estaContenido)
            {
                pesos[0] = 0;
                pesos[1] = 0;
                pesos[2] = 0;
                pesos[3] = 0;
            }
            else
            {
                pesos[0] = (Mathf.Abs(Vector3.Dot(Vector3.Cross(elementos[1] - verticeMundo, elementos[2] - verticeMundo), elementos[3] - verticeMundo) / 6)) / volumeTotal;
                pesos[1] = (Mathf.Abs(Vector3.Dot(Vector3.Cross(verticeMundo - elementos[0], elementos[2] - elementos[0]), elementos[3] - elementos[0]) / 6)) / volumeTotal;
                pesos[2] = (Mathf.Abs(Vector3.Dot(Vector3.Cross(elementos[1] - elementos[0], verticeMundo - elementos[0]), elementos[3] - elementos[0]) / 6)) / volumeTotal;
                pesos[3] = (Mathf.Abs(Vector3.Dot(Vector3.Cross(elementos[1] - elementos[0], elementos[2] - elementos[0]), verticeMundo - elementos[0]) / 6)) / volumeTotal;
            }
            weight.Add(pesos);
            contador = 0;

        }



    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
            this.Paused = !this.Paused;

        foreach (Spring muelle in springs.Values)
        {
            Debug.DrawLine(muelle.nodeA.Pos, muelle.nodeB.Pos, Color.blue);
        }
        //foreach (Spring muelle in springs)
        //{
        //    Debug.DrawLine(muelle.nodeA.Pos, muelle.nodeB.Pos, Color.blue);
        //}

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
        foreach (Node node in nodes)
        {
            node.Force = Vector3.zero;
        }
        foreach (Node node in nodes)
        {
            node.AddForces(alpha);
        }

        foreach (Spring muelle in springs.Values)
        {
            muelle.AddForces(beta);
        }
        //foreach (Spring spring in springs)
        //{
        //    spring.AddForces(beta);
        //}
        foreach (Node node in nodes)
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
        foreach (Node node in nodes)
        {
            node.Force = Vector3.zero;
        }
        foreach (Node node in nodes)
        {
            node.AddForces(alpha);
        }

        foreach (Spring spring in springs.Values)
        {
            spring.AddForces(beta);
        }
        //foreach (Spring spring in springs)
        //{
        //    spring.AddForces(beta);
        //}


        foreach (Node node in nodes)
        {
            if (!node.Fixed)
            {
                node.Vel += TimeStep * node.Force / node.Mass;
                node.Pos += TimeStep * node.Vel;
            }
        }
        ActualizarNodos();
    }

    private void ActualizarNodos()
    {

        //Calculamos la posición de los nodos según los pesos
        Vector3[] nuevosVertices = new Vector3[vertices.Length];
        for (int i = 0; i < contenido.Count; i++)
        {
            if (contenido.ContainsKey(i))
            {
                nuevosVertices[i] = weight[i][0] * nodes[(int)Tetraedros[contenido[i]].x].Pos;
                nuevosVertices[i] += weight[i][1] * nodes[(int)Tetraedros[contenido[i]].y].Pos;
                nuevosVertices[i] += weight[i][2] * nodes[(int)Tetraedros[contenido[i]].z].Pos;
                nuevosVertices[i] += weight[i][3] * nodes[(int)Tetraedros[contenido[i]].w].Pos;

                nuevosVertices[i] = transform.InverseTransformPoint(nuevosVertices[i]);
            }
            else
            {
                nuevosVertices[i] = vertices[i];
            }

        }
        malla.vertices = nuevosVertices;
        foreach (Spring muelle in springs.Values)
        {
            muelle.ActualizarLongitud();
        }
        //for (int i = 0; i < springs.Count; i++)
        //{
        //    springs[i].ActualizarLongitud();
        //}
        malla.RecalculateNormals();
    }

    private Vector3 CalcularViento(Node a, Node b, Node c, Vector3 viento, float k)
    {
        Vector3 cross = Vector3.Cross((b.Pos - a.Pos), (c.Pos - a.Pos));
        Vector3 normalTriangulo = cross.normalized;
        float areaTriangulo = cross.magnitude / 2;
        return (Vector3.Dot(normalTriangulo, viento) * k * areaTriangulo / 3) * normalTriangulo;
    }

    //Función para comprobar que un punto está dentro del tetraedro
    public bool inVolume(Vector3 Pos, Vector3[] PosV)
    {

        return SameSide(PosV[0], PosV[1], PosV[2], PosV[3], Pos) && SameSide(PosV[1], PosV[2], PosV[3], PosV[0], Pos) && SameSide(PosV[2], PosV[3], PosV[0], PosV[1], Pos)
            && SameSide(PosV[3], PosV[0], PosV[1], PosV[2], Pos);

    }


    bool SameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p)
    {
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);
        float dotV4 = Vector3.Dot(normal, v4 - v1);
        float dotP = Vector3.Dot(normal, p - v1);
        return Math.Sign(dotV4) == Math.Sign(dotP);
    }
}
