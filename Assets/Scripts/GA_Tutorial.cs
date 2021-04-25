using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GA_Tutorial : MonoBehaviour
{
    [Range(0f, 1f)]
    public float mutationRate;

    public int populationSize;
    public float lifeSpan;
    public Text infoText;
    public GameObject rocket;

    //Class for Population
    Population pop;

    int lifeCycle;
    float recordTime;

    //Class for Obstacles
    List<Obstacle> obstacles;
    Obstacle target;

    Vector2 screenSize;
    void Start()
    {
        SetUpCam();

        lifeCycle = 0;
        recordTime = lifeSpan;

        target = new Obstacle(0, screenSize.y - 2f, 4f, 3f);

        pop = new Population(rocket, screenSize, mutationRate, populationSize, lifeSpan, target);

        obstacles = new List<Obstacle>();
        obstacles.Add(new Obstacle(0, 0, 12, 2));
        obstacles.Add(new Obstacle(0, 2, 3, 6));
        obstacles.Add(new Obstacle(15, 5, 5, 20));
        obstacles.Add(new Obstacle(-15, 5, 5, 20));
        obstacles.Add(new Obstacle(10, 8, 10, 5));
        obstacles.Add(new Obstacle(-10, 8, 10, 5));
    }

    // Update is called once per frame
    void Update()
    {
        if(lifeCycle < lifeSpan)
        {
            pop.Live(obstacles);
            if (pop.TargetReached() && lifeCycle < recordTime)
                recordTime = lifeCycle;
            lifeCycle++;
        }
        else
        {
            lifeCycle = 0;
            pop.Fitness();
            pop.Selection();
            pop.Reproduction();
        }

        infoText.text = $"Generation #: {pop.Generation}\n" +
                        $"Cycles Left: {lifeSpan - lifeCycle}\n" +
                        $"Record Cycles: {recordTime}";
    }

    void SetUpCam()
    {
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 10;
        screenSize = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height));
    }
}

public class Rocket
{
    GameObject gobj;

    Vector2 pos;
    Vector2 velocity;
    Vector2 accel;

    float recordDist;

    public float fitness { get; private set; }
    public DNA dna { get; private set; }

    int geneCounter = 0;

    public bool hitObstacle { get; private set; }
    public bool hitTarget { get; private set; }

    float finishTime;

    Obstacle target;

    public Rocket(GameObject rocket, Vector2 l, DNA _dna, Obstacle ob, int totalRockets)
    {
        hitTarget = false;
        target = ob;
        accel = Vector2.zero;
        velocity = Vector2.zero;
        pos = l;
        dna = _dna;
        finishTime = 0;
        recordDist = 1000000000;
        hitObstacle = false;
        gobj = GameObject.Instantiate(rocket, pos, Quaternion.identity);
    }

    public void CalculateFitness()
    {
        if(recordDist < 1)
        {
            recordDist = 1;
        }

        fitness = (1 / (finishTime * recordDist));
        fitness = Mathf.Pow(fitness, 5);

        if (hitObstacle)
        {
            fitness *= .01f;
        }
        if (hitTarget)
        {
            fitness *= 3f;
        }
    }

    public void Run(List<Obstacle> os)
    {
        if(!hitObstacle && !hitTarget)
        {
            ApplyForce(dna.Genes[geneCounter]);
            geneCounter = (geneCounter + 1) % dna.Genes.Length;
            Update();

            obstacles(os);
        }

        if (!hitObstacle)
        {
            Display();
        }
        else
        {
            gobj.SetActive(false);
        }
    }

    public void CheckTarget()
    {
        float d = Vector2.Distance(pos, target.pos);
        if(d < recordDist)
        {
            recordDist = d;
        }

        if(target.Contains(pos) && !hitTarget)
        {
            hitTarget = true;
        }else if (!hitTarget)
        {
            finishTime++;
        }
    }

    void obstacles(List<Obstacle> os)
    {
        foreach(Obstacle o in os)
        {
            if (o.Contains(pos))
                hitObstacle = true;
        }
    }

    void ApplyForce(Vector2 f)
    {
        accel += f;
    }

    void Update()
    {
        velocity += accel;
        pos += velocity;
        accel *= 0;
    }

    void Display()
    {
        float theta = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        gobj.transform.position = pos;
        gobj.transform.rotation = Quaternion.Euler(gobj.transform.rotation.x, gobj.transform.rotation.y, gobj.transform.rotation.z + theta - 90);
    }

    public void Death()
    {
        Object.Destroy(this.gobj.gameObject);
    }
}

public class DNA
{
    public Vector2[] Genes { get; private set; }

    float maxForce = 0.01f;

    public DNA(float lifeTime)
    {
        Genes = new Vector2[(int)lifeTime];
        for(int i = 0; i < Genes.Length; i++)
        {
            float angle = Random.Range(0f, 360f);
            Genes[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Genes[i] *= Random.Range(0f, maxForce);
        }
    }

    public DNA(Vector2[] newGenes)
    {
        Genes = newGenes;
    }

    public DNA CrossOver(DNA partner)
    {
        Vector2[] child = new Vector2[Genes.Length];

        int crossover = Random.Range(0, Genes.Length);
        for(int i =0; i < Genes.Length; i++)
        {
            if( i > crossover)
            {
                child[i] = Genes[i];
            }
            else
            {
                child[i] = partner.Genes[i];
            }
        }
        DNA newGenes = new DNA(child);
        return newGenes;
    }

    public void Mutate(float m)
    {
        for(int i =0; i< Genes.Length; i++)
        {
            if(Random.Range(0f,1f) < m)
            {
                float angle = Random.Range(0f, 360f);
                Genes[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Genes[i] *= Random.Range(0f, maxForce);
            }
        }
    }

}

public class Obstacle
{
    public Vector2 pos { get; private set; }
    float w, h;
    public Obstacle(float x, float y, float _w, float _h)
    {
        pos = new Vector2(x, y);
        w = _w;
        h = _h;
        SpawnObstacle();
    }

    void SpawnObstacle()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(w, h);
    }

    public bool Contains(Vector2 spot)
    {
        if (spot.x < (pos.x + (w / 2)) && spot.x > (pos.x - (w / 2))
            && spot.y < (pos.y + (h / 2)) && spot.y > (pos.y - (h / 2)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

public class Population
{
    float mutationRate;
    Rocket[] pop;
    List<Rocket> matingPool;
    public int Generation { get; private set; }
    Vector2 screenSize;
    Obstacle target;
    GameObject r;

    public Population(GameObject _rocket, Vector2 screen, float mutation, int numRockets, float lifeSpan, Obstacle obTarget)
    {
        r = _rocket;
        mutationRate = mutation;
        pop = new Rocket[numRockets];
        matingPool = new List<Rocket>();
        Generation = 0;
        screenSize = screen;
        target = obTarget;

        for(int i = 0; i < pop.Length; i++)
        {
            Vector2 pos = new Vector2(0, -screenSize.y);
            pop[i] = new Rocket(_rocket, pos, new DNA(lifeSpan), target, pop.Length);
        }
    }

    public void Live(List<Obstacle> os)
    {
        for(int i = 0; i < pop.Length; i++)
        {
            pop[i].CheckTarget();
            pop[i].Run(os);
        }
    }

    public bool TargetReached()
    {
        for(int i = 0; i < pop.Length; i++)
        {
            if (pop[i].hitTarget)
                return true;

        }
        return false;
    }

    public void Fitness()
    {
        for(int i = 0; i <pop.Length; i++)
        {
            pop[i].CalculateFitness();
        }
    }

    public void Selection()
    {
        matingPool.Clear();

        float maxFitness = GetMaxFitness();

        for(int i = 0; i < pop.Length; i++)
        {
            float fitnessNormal = 0 + (pop[i].fitness - 0) * (1 - 0) / (maxFitness - 0);
            int n = (int)fitnessNormal * 100;
            for(int j = 0; j < n; j++)
            {
                matingPool.Add(pop[i]);
            }
        }
    }

    public void Reproduction()
    {
        for (int i = 0; i < pop.Length; i++){
            pop[i].Death();

            int m = Random.Range(0, matingPool.Count);
            int d = Random.Range(0, matingPool.Count);

            Rocket mom = matingPool[m];
            Rocket dad = matingPool[d];

            DNA momGenes = mom.dna;
            DNA dadGenes = dad.dna;

            DNA child = momGenes.CrossOver(dadGenes);

            child.Mutate(mutationRate);

            Vector2 pos = new Vector2(0f, -screenSize.y);
            pop[i] = new Rocket(r, pos, child, target, pop.Length);
        }

        Generation++;
    }

    float GetMaxFitness()
    {
        float record = 0f;
        for(int i = 0; i < pop.Length; i++)
        {
            if (pop[i].fitness > record)
            {
                record = pop[i].fitness;
            }
        }
        return record;
    }
}
