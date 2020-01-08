using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;


public class ParticleLifeSystem : JobComponentSystem
{
    public ParticleLife particleLife;
    public float lastSimulationDeltaTime;
    public int lastNumCells;

    public int maxCellSize;
    public int numOccupiedCells;
    public float averageCellSize;
    public int numCellAccesses;
    public int numInteractionCandidates;
    public int numActualInteractions;

    //public int lastNumCells;


    [BurstCompile]
    struct ParticleLifeSystemJob : IJobForEach<Translation, Particle>
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> entities;

        public float deltaTime;
        public float friction;

        public float3 lowerBound;
        public float3 upperBound;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public float bounce;

        //[ReadOnly] public ComponentDataFromEntity<Translation> translations;
        //[ReadOnly] public ComponentDataFromEntity<Particle> particles;

        public void Execute(ref Translation translation, ref Particle particle)
        {
            
            #region movement
            translation.Value = translation.Value + particle.vel * deltaTime;
            #endregion
            #region friction
            particle.vel = particle.vel * friction;
            #endregion

            #region check universe bounds
            float3 dim = upperBound - lowerBound;
            #region x dimension
            if (wrapX && dim.x > 0) 
            {
                while (translation.Value.x < lowerBound.x) translation.Value.x += dim.x;
                while (translation.Value.x > upperBound.x) translation.Value.x -= dim.x;
            }
            else
            {
                if (translation.Value.x < lowerBound.x)
                {
                    translation.Value.x = lowerBound.x;
                    particle.vel.x *= -bounce;
                }
                else if (translation.Value.x > upperBound.x)
                {
                    translation.Value.x = upperBound.x;
                    particle.vel.x *= -bounce;
                }
            }
            #endregion
            #region y dimension
            if (wrapY && dim.y > 0)
            {
                while (translation.Value.y < lowerBound.y) translation.Value.y += dim.y;
                while (translation.Value.y > upperBound.y) translation.Value.y -= dim.y;
            }
            else
            {
                if (translation.Value.y < lowerBound.y)
                {
                    translation.Value.y = lowerBound.y;
                    particle.vel.y *= -bounce;
                }
                else if (translation.Value.y > upperBound.y)
                {
                    translation.Value.y = upperBound.y;
                    particle.vel.y *= -bounce;
                }
            }
            #endregion
            #region z dimension
            if (wrapZ && dim.z > 0)
            {
                while (translation.Value.z < lowerBound.z) translation.Value.z += dim.z;
                while (translation.Value.z > upperBound.z) translation.Value.z -= dim.z;
            }
            else
            {
                if (translation.Value.z < lowerBound.z)
                {
                    translation.Value.z = lowerBound.z;
                    particle.vel.z *= -bounce;
                }
                else if (translation.Value.z > upperBound.z)
                {
                    translation.Value.z = upperBound.z;
                    particle.vel.z *= -bounce;
                }
            }
            #endregion
            #endregion
        }
    }

    [BurstCompile]
    struct ParticleLifeSystemJobFor : IJobParallelFor
    {
        //[DeallocateOnJobCompletion, ReadOnly]
        //public NativeArray<Entity> entities;

        public float dt;
        public float friction;
        public float frictionTime;
        public float maxSpeed;

        public float3 lowerBound;
        public float3 upperBound;
        public float3 dim;
        public float3 halfDim;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public float bounce;
        public float eps;
        public float strength;
        public float r_smooth;
        public bool flatForce;
        public int numTypes;
        [ReadOnly] public NativeArray<float> Attract;
        [ReadOnly] public NativeArray<float> RangeMin;
        [ReadOnly] public NativeArray<float> RangeMax;
        public float3 gravityTarget;
        public float gravityTargetRange;
        public bool gravityLinear;
        public float gravityStrength;
        public float maxGravity;

        //public ComponentDataFromEntity<Translation> translations;
        //public ComponentDataFromEntity<Particle> particles;
        [ReadOnly] public NativeArray<Translation> oldTranslations;
        [ReadOnly] public NativeArray<Particle> oldParticles;
        public NativeArray<Translation> translations;
        public NativeArray<Particle> particles;


        //[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
        //public ArchetypeChunkComponentType<Translation> TranslationType;
        //public ArchetypeChunkComponentType<Particle> ParticleType;
        private int C(int x, int y) => y * numTypes + x;

        public GridMeta grid;

        // contains cell numbers for each particle
        // Length == num_particles
        [ReadOnly] public NativeArray<int> cellNumbers;

        // contains all particle indices grouped by cell in contigous blocks
        // Length == num_particles
        [ReadOnly] public NativeArray<int> cellContents;

        #region cell size and position inside cellContents
        // contains start index into cellContents array for each cell
        // Length == num_cells
        [ReadOnly] public NativeArray<int> cellStarts;
        //// contains cell capacities for each cell, i.e. how many particles will be contained
        //// Length == num_cells
        //[ReadOnly] public NativeArray<int> cellCapacities;
        // contains cell size for each cell, i.e. how many particles are contained so far
        // Length == num_cells
        [ReadOnly] public NativeArray<int> cellSizes;
        // contains current seq number for each cell, 
        // is used to check the age of cell and discard not freshly updated ones (or treat them as zero)
        // Length == num_cells
        [ReadOnly] public NativeArray<int> cellSeqNums;
        #endregion

        // contains neighborhood cell numbers grouped by particle in contigous blocks of 27 (that is 3x3x3)
        // Length == num_particles * 27
        [ReadOnly] public NativeArray<int> cellNeighborhoodContents;
        // start index into cellNeighborhoodContents array for each particle
        // is particle_index * 27

        // contains actual number of neighborhood (including self) cells for each particle
        // due to world bounds this can be less than 27
        // Length == num_particles
        [ReadOnly] public NativeArray<int> cellNeighborhoodSizes;

        // Length == 1
        //public NativeArray<GridStats> stats;

        // Length == 1
        [NativeDisableParallelForRestriction]
        public NativeArray<int> numInteractionCandidates;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> numActualInteractions;

        public void Execute(int index)
        {
            Translation translation = oldTranslations[index];
            Particle particle = oldParticles[index];

            
            //int numInteractionCandidates = 0;
            //int numActualInteractions = 0;
            #region particle interaction
            if (numTypes > 0)
            {
                // iterate over cells in 3x3x3 neighborhood including our cell
                // the iterate over each particle in the cells 
                // check interactions with each particle
                int cellNum = cellNumbers[index];
                for (int i = 0; i < cellNeighborhoodSizes[index]; i++)
                {
                    int neighborCellNum = cellNeighborhoodContents[index * 27 + i];
                    int cellStart, cellSize, cellSeqNum;
                    cellSeqNum = cellSeqNums[neighborCellNum];
                    // treat cell as zero if it is too old
                    if (cellSeqNum < grid.seqNum)
                    {
                        cellStart = 0;
                        cellSize = 0;
                    }
                    else
                    {
                        cellStart = cellStarts[neighborCellNum];
                        cellSize = cellSizes[neighborCellNum];
                    }
                    // iterate particles in cell
                    for (int j = 0; j < cellSize; j++)
                    {
                        int k = cellContents[cellStart + j];
                        
                        numInteractionCandidates[0]++; // ECS samples state in code that this is threadsafe

                        //int num = particles.Length;
                        //for (int k = 0; k < num; k++)
                        //{

                        // no self interaction
                        if (k == index) continue;
                        Translation otherTranslation = oldTranslations[k];
                        Particle otherParticle = oldParticles[k];
                        float3 diff = otherTranslation.Value - translation.Value;
                        #region wrap world
                        if (wrapX && dim.x > 0)
                        {
                            while (diff.x < -halfDim.x) diff.x += dim.x;
                            while (diff.x > +halfDim.x) diff.x -= dim.x;
                        }
                        if (wrapY && dim.y > 0)
                        {
                            while (diff.y < -halfDim.y) diff.y += dim.y;
                            while (diff.y > +halfDim.y) diff.y -= dim.y;
                        }
                        if (wrapZ && dim.z > 0)
                        {
                            while (diff.z < -halfDim.z) diff.z += dim.z;
                            while (diff.z > +halfDim.z) diff.z -= dim.z;
                        }
                        #endregion
                        float r2 = diff.x * diff.x + diff.y * diff.y + diff.z * diff.z;
                        var typeA = particle.type % numTypes;
                        var typeB = otherParticle.type % numTypes;
                        var coord = C(typeA, typeB);
                        float r_max = RangeMax[coord];
                        if (r2 > r_max * r_max) continue;
                        float r = math.sqrt(r2);
                        if (r < eps) continue;
                        float3 direction = diff / r;
                        var r_min = RangeMin[coord];
                        float f;
                        #region force calculation
                        if (r > r_min)
                        {
                            var attract = Attract[coord];
                            if (flatForce)
                            {
                                f = attract;
                            }
                            else
                            {
                                var rangeMid = (r_min + r_max) * 0.5f;
                                var numer = 2.0f * math.abs(r - rangeMid);
                                var denom = r_max - r_min;
                                if (denom > eps)
                                {
                                    f = attract * (1.0f - numer / denom);
                                }
                                else
                                {
                                    f = attract;
                                }
                            }
                        }
                        else
                        {
                            if ((r + r_smooth > eps) && (r_min + r_smooth > eps))
                            {
                                f = r_smooth * r_min * (1.0f / (r_min + r_smooth) - 1.0f / (r + r_smooth));
                            }
                            else
                            {
                                f = 0f;
                            }
                        }
                        #endregion
                        particle.vel = particle.vel + direction * f * strength * dt;
                        //otherParticle.vel = otherParticle.vel - direction * dt * f;
                        //particles[k] = otherParticle;
                        numActualInteractions[0]++; // ECS samples state in code that this is threadsafe

                    }
                }
                //}
            }
            #endregion

            #region gravity
            {
                float3 diff = gravityTarget - translation.Value;
                float range = math.sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z);
                if (range > eps)
                {
                    float3 direction = diff / range;
                    float rangeDiff = gravityTargetRange - range;
                    if (abs(rangeDiff) > eps)
                    {
                        float g = gravityStrength / (gravityLinear ? -rangeDiff : (rangeDiff * rangeDiff));
                        if (!gravityLinear && rangeDiff > 0) g = -g;
                        if (g < -abs(maxGravity)) g = -abs(maxGravity);
                        if (g > +abs(maxGravity)) g = +abs(maxGravity);
                        particle.vel = particle.vel + direction * g * dt;
                    }
                }
            }

            #endregion

            #region movement
            translation.Value = translation.Value + particle.vel * dt;
            #endregion

            #region friction
            // from ParticleLife:
            //   particle.vel *= friction
            // friction is actually exponentiall weighted gain filter:
            //   particle.vel = 0 * gain + (1-gain) *  particle.vel
            //   with gain == 1-friction
            // to allow variable step rate the gain(i.e. friction) value must be given for a reference time step 'frictionTime'
            // velocity will be friction*velocity after frictionTime seconds

            // dont apply friction when friction==1 or no time passes 
            if ((abs(1-friction)) > eps && (abs(dt) > eps))
            {
                if (abs(friction) < eps) particle.vel = 0;
                else  
                {
                    // convert exponentiall weighted gain filter to different time step
                    float friction_gain0_1s = 1 - friction;
                    float friction_gain0_corrected = dt / (frictionTime * (1 - friction_gain0_1s) / friction_gain0_1s + dt);
                    // and apply it, omit 0*gain
                    particle.vel = particle.vel * (1 - friction_gain0_corrected);
                }
            }
            #endregion

            #region check universe bounds
            #region x dimension
            if (wrapX && dim.x > 0) 
            {
                while (translation.Value.x < lowerBound.x) translation.Value.x += dim.x;
                while (translation.Value.x > upperBound.x) translation.Value.x -= dim.x;
            }
            else
            {
                if (translation.Value.x < lowerBound.x)
                {
                    translation.Value.x = lowerBound.x;
                    particle.vel.x *= -bounce;
                }
                else if (translation.Value.x > upperBound.x)
                {
                    translation.Value.x = upperBound.x;
                    particle.vel.x *= -bounce;
                }
            }
            #endregion
            #region y dimension
            if (wrapY && dim.y > 0)
            {
                while (translation.Value.y < lowerBound.y) translation.Value.y += dim.y;
                while (translation.Value.y > upperBound.y) translation.Value.y -= dim.y;
            }
            else
            {
                if (translation.Value.y < lowerBound.y)
                {
                    translation.Value.y = lowerBound.y;
                    particle.vel.y *= -bounce;
                }
                else if (translation.Value.y > upperBound.y)
                {
                    translation.Value.y = upperBound.y;
                    particle.vel.y *= -bounce;
                }
            }
            #endregion
            #region z dimension
            if (wrapZ && dim.z > 0)
            {
                while (translation.Value.z < lowerBound.z) translation.Value.z += dim.z;
                while (translation.Value.z > upperBound.z) translation.Value.z -= dim.z;
            }
            else
            {
                if (translation.Value.z < lowerBound.z)
                {
                    translation.Value.z = lowerBound.z;
                    particle.vel.z *= -bounce;
                }
                else if (translation.Value.z > upperBound.z)
                {
                    translation.Value.z = upperBound.z;
                    particle.vel.z *= -bounce;
                }
            }
            #endregion
            #endregion

            #region max speed
            float speed = math.sqrt(particle.vel.x * particle.vel.x + particle.vel.y * particle.vel.y + particle.vel.z * particle.vel.z);
            if (speed > eps)
                if (speed > maxSpeed)
                {
                    particle.vel.x *= maxSpeed / speed;
                    particle.vel.y *= maxSpeed / speed;
                    particle.vel.z *= maxSpeed / speed;
                }
            #endregion
            translations[index] = translation;
            particles[index] = particle;
        }
    }

    public struct GridStats
    {
        public int maxCellSize;
        public int numOccupiedCells;
        public float averageCellSize;
        public int numCellAccesses;
    };

    [BurstCompile]
    public struct GridMeta
    {
        public float3 lowerBound;
        public float3 dim;
        public float cellSize;
        public int3 gridSize;
        public bool wrapX;
        public bool wrapY;
        public bool wrapZ;
        public int numCells;
        public int seqNum;

        public int coord_xyz(float x, float y, float z)
        {
            int u = (int)((x - lowerBound.x) / cellSize);
            int v = (int)((y - lowerBound.y) / cellSize);
            int w = (int)((z - lowerBound.z) / cellSize);
            u = u % gridSize.x;
            v = v % gridSize.y;
            w = w % gridSize.z;
            if (u < 0) u += gridSize.x;
            if (v < 0) v += gridSize.y;
            if (w < 0) w += gridSize.z;
            return coord_uvw(u, v, w);
        }
        public int coord_uvw(int u, int v, int w)
        {
            return w * (gridSize.x * gridSize.y) + (v * gridSize.x + u);
        }
        public void icoord_uvw(int k, out int out_u, out int out_v, out int out_w)
        {
            out_w = k / (gridSize.x * gridSize.y);
            out_v = (k - out_w * (gridSize.x * gridSize.y)) / gridSize.x;
            out_u = k % gridSize.x;
        }

        public bool bcheck_u(ref int u)
        {
            if (wrapX)
            {
                u = u % gridSize.x;
                if (u < 0) u += gridSize.x;
            }
            else
            {
                if (u < 0) return false;
                if (u >= gridSize.x) return false;
            }
            return true;
        }
        public bool bcheck_v(ref int v)
        {
            if (wrapY)
            {
                v = v % gridSize.y;
                if (v < 0) v += gridSize.y;
            }
            else
            {
                if (v < 0) return false;
                if (v >= gridSize.y) return false;
            }
            return true;
        }
        public bool bcheck_w(ref int w)
        {
            if (wrapZ)
            {
                w = w % gridSize.z;
                if (w < 0) w += gridSize.z;
            }
            else
            {
                if (w < 0) return false;
                if (w >= gridSize.z) return false;
            }
            return true;
        }
    }

    [BurstCompile]
    struct FillGridJob : IJob
    {
        public GridMeta grid;


        [ReadOnly] public NativeArray<Translation> translations;

        // contains cell numbers for each particle
        // Length == num_particles
        public NativeArray<int> cellNumbers;

        // contains all particle indices grouped by cell in contigous blocks
        // Length == num_particles
        public NativeArray<int> cellContents;

        #region cell size and position inside cellContents
        // contains start index into cellContents array for each cell
        // Length == num_cells
        public NativeArray<int> cellStarts;
        // contains cell capacities for each cell, i.e. how many particles will be contained
        // Length == num_cells
        public NativeArray<int> cellCapacities;
        // contains cell size for each cell, i.e. how many particles are contained so far
        // Length == num_cells
        public NativeArray<int> cellSizes;
        // contains current seq number for each cell, 
        // is used to check the age of cell and discard not freshly updated ones (or treat them as zero)
        // Length == num_cells
        public NativeArray<int> cellSeqNums;
        #endregion

        // contains neighborhood cell numbers grouped by particle in contigous blocks of 27 (that is 3x3x3)
        // Length == num_particles * 27
        public NativeArray<int> cellNeighborhoodContents;
        // start index into cellNeighborhoodContents array for each particle
        // is particle_index * 27

        // contains actual number of neighborhood (including self) cells for each particle
        // due to world bounds this can be less than 27
        // Length == num_particles
        public NativeArray<int> cellNeighborhoodSizes;
        

        // Length == 1
        public NativeArray<GridStats> stats;



        public bool thisShouldNotHappen;
        public void Execute()
        {
            thisShouldNotHappen = false;
            GridStats _stats = new GridStats()
            {
                maxCellSize = 0,
                averageCellSize = 0,
                numCellAccesses = 0,
                numOccupiedCells = 0,
            };
            int numParticles = translations.Length;

            // check all cells zero
            //bool allZero = true;
            //for (int k = 0; k < numCells; k++)
            //{
            //    allZero = allZero
            //        && (cellStarts[k] == 0)
            //        && (cellCapacities[k] == 0)
            //        && (cellSizes[k] == 0);
            //}
            //if (!allZero)
            //    thisShouldNotHappen = true;

            // compute all cell numbers and set relevant cells to zero
            for (int i = 0; i < numParticles; i++)
            {
                var pos = translations[i].Value;
                int cellNumber = grid.coord_xyz(pos.x, pos.y, pos.z);
                cellNumbers[i] = cellNumber;
                #region compute neighborhood and zero it
                // whole 3x3 neighborhood could be accessed, so zero it
                // also insert neighborhoods for later use
                cellNeighborhoodSizes[i] = 0;
                int u, v, w;
                grid.icoord_uvw(cellNumber, out u, out v, out w);
                int uu, vv, ww;
                int k;
                for (int dw=-1;dw<=+1;dw++)
                {
                    ww = w + dw;
                    if (!grid.bcheck_w(ref ww)) continue;
                    for (int dv=-1;dv<=+1;dv++)
                    {
                        vv = v + dv;
                        if (!grid.bcheck_v(ref vv)) continue;
                        for (int du=-1;du<=+1;du++)
                        {
                            uu = u + du;
                            if (!grid.bcheck_u(ref uu)) continue;
                            k = grid.coord_uvw(uu, vv, ww);
                            // todo remove later, just to make sure bounds checks per coordinate really works
                            if (k < 0 && k >= grid.numCells)
                            {
                                thisShouldNotHappen = true;
                                continue;
                            }

                            #region insert cell into neighborhoods
                            cellNeighborhoodContents[i * 27 + cellNeighborhoodSizes[i]] = k;
                            cellNeighborhoodSizes[i]++;
                            #endregion

                            #region zero the cell and update its age
                            // zero the cell and update its age to the current seqNum
                            // when others find cells with old seqNum they should assume
                            // it was not zeroed due too lazyness (improve performance)
                            // and treat it as zero
                            if (cellSeqNums[k] < grid.seqNum)
                            {
                                cellStarts[k] = 0;
                                cellCapacities[k] = 0;
                                cellSizes[k] = 0;
                                cellSeqNums[k] = grid.seqNum;
                                _stats.numCellAccesses++;
                            }
                            #endregion

                        }
                    }
                }
                #endregion
            }
            
            // compute cell capacities
            for (int i = 0; i < numParticles; i++)
            {
                _stats.numCellAccesses++;
                cellCapacities[cellNumbers[i]] += 1;
            }

            int nextContentIndex=0;
            // insert particles indices, "allocate" cells in cellContents on the fly
            for (int i = 0; i < numParticles; i++)
            {
                int k = cellNumbers[i];
                if (cellSizes[k] == 0)
                {
                    // allocate cell
                    cellStarts[k] = nextContentIndex;
                    nextContentIndex += cellCapacities[k];
                    // stats
                    if (_stats.maxCellSize < cellCapacities[k])
                    {
                        _stats.maxCellSize = cellCapacities[k];
                    }
                    _stats.averageCellSize += cellCapacities[k];
                    _stats.numOccupiedCells++;
                    _stats.numCellAccesses++;
                }
                int insertIndex = cellStarts[k] + cellSizes[k];
                // todo: remove later
                if (insertIndex >= cellContents.Length)
                {
                    thisShouldNotHappen = true;
                }

                cellContents[insertIndex] = i;
                cellSizes[k]++;
                _stats.numCellAccesses++;
            }
            _stats.averageCellSize /= _stats.numOccupiedCells;
            stats[0] = _stats;

        }
    }
    EntityQuery m_query;

    public int numTypes
    {
        get { return m_newNumTypes; }
        set
        {
            m_newNumTypes = value;
            m_NumTypesChanged = true;
        }
    }
    public NativeArray<float> Attract
    {
        get { return m_newAttract; }
        set
        {
            m_newAttract = value;
            m_AttractChanged = true;
        }
    }
    public NativeArray<float> RangeMin
    {
        get { return m_newRangeMin; }
        set
        {
            m_newRangeMin = value;
            m_RangeMinChanged = true;
        }
    }
    public NativeArray<float> RangeMax
    {
        get { return m_newRangeMax; }
        set
        {
            m_newRangeMax = value;
            m_RangeMaxChanged = true;
        }
    }

    bool m_NumTypesChanged = false;
    bool m_AttractChanged = false;
    bool m_RangeMinChanged = false;
    bool m_RangeMaxChanged = false;
    int m_newNumTypes;
    NativeArray<float> m_newAttract;
    NativeArray<float> m_newRangeMin;
    NativeArray<float> m_newRangeMax;
    int m_copyOfNumTypes = 0;
    NativeArray<float> m_copyOfAttract = new NativeArray<float>(0, Allocator.Persistent);
    NativeArray<float> m_copyOfRangeMin = new NativeArray<float>(0, Allocator.Persistent);
    NativeArray<float> m_copyOfRangeMax = new NativeArray<float>(0, Allocator.Persistent);

    protected override void OnCreate()
    {
        var queryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Translation), typeof(Particle) }
        };
        m_query = GetEntityQuery(queryDesc);
    }
    protected override void OnDestroy()
    {
        m_copyOfAttract.Dispose();
        m_copyOfRangeMin.Dispose();
        m_copyOfRangeMax.Dispose();
        m_cellStarts.Dispose();
        m_cellCapacities.Dispose();
        m_cellSizes.Dispose();
        m_cellSeqNums.Dispose();
    }

    private NativeArray<int> m_cellStarts;
    private NativeArray<int> m_cellCapacities;
    private NativeArray<int> m_cellSizes;
    private NativeArray<int> m_cellSeqNums;
    private GridMeta m_grid = new GridMeta();

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        #region assign new particle type info in sync with the jobs
        if (m_NumTypesChanged)
        {
            m_copyOfNumTypes = numTypes;
            m_NumTypesChanged = false;
        }
        if (m_AttractChanged && Attract.IsCreated)
        {
            if (m_copyOfAttract.IsCreated || m_copyOfAttract.Length > 0) m_copyOfAttract.Dispose();
            m_copyOfAttract = new NativeArray<float>(Attract, Allocator.Persistent);
            m_AttractChanged = false;
        }
        if (m_RangeMinChanged && RangeMin.IsCreated)
        {
            if (m_copyOfRangeMin.IsCreated || m_copyOfRangeMin.Length > 0) m_copyOfRangeMin.Dispose();
            m_copyOfRangeMin = new NativeArray<float>(RangeMin, Allocator.Persistent);
            m_RangeMinChanged = false;
        }
        if (m_RangeMaxChanged && RangeMax.IsCreated)
        {
            if (m_copyOfRangeMax.IsCreated || m_copyOfRangeMax.Length > 0) m_copyOfRangeMax.Dispose();
            m_copyOfRangeMax = new NativeArray<float>(RangeMax, Allocator.Persistent);
            m_RangeMaxChanged = false;
        }
        //// only do jobs when particle type info is existing and consistent
        //if ((!m_copyOfAttract.IsCreated || !m_copyOfRangeMin.IsCreated || !m_copyOfRangeMax.IsCreated)
        //    || (m_copyOfAttract.Length != m_copyOfNumTypes * m_copyOfNumTypes)
        //    || (m_copyOfRangeMin.Length != m_copyOfNumTypes * m_copyOfNumTypes)
        //    || (m_copyOfRangeMax.Length != m_copyOfNumTypes * m_copyOfNumTypes))
        //    return inputDependencies;
        #endregion

        #region update grid meta
        if (m_grid.seqNum <= 0) m_grid.seqNum = 1;
        m_grid.lowerBound = particleLife.settings.lowerBound;
        m_grid.cellSize = particleLife.settings.cellSize;

        if (m_grid.cellSize <= 0) m_grid.cellSize = 50.0f;

        m_grid.dim = particleLife.settings.upperBound - particleLife.settings.lowerBound;
        m_grid.gridSize.x = (int)math.ceil(m_grid.dim.x / m_grid.cellSize);
        m_grid.gridSize.y = (int)math.ceil(m_grid.dim.y / m_grid.cellSize);
        m_grid.gridSize.z = (int)math.ceil(m_grid.dim.z / m_grid.cellSize);
        if (m_grid.gridSize.x < 1) m_grid.gridSize.x = 1;
        if (m_grid.gridSize.y < 1) m_grid.gridSize.y = 1;
        if (m_grid.gridSize.z < 1) m_grid.gridSize.z = 1;
        m_grid.wrapX = particleLife.settings.wrapX;
        m_grid.wrapY = particleLife.settings.wrapY;
        m_grid.wrapZ = particleLife.settings.wrapZ;
        m_grid.numCells = m_grid.gridSize.x * m_grid.gridSize.y * m_grid.gridSize.z;
        lastNumCells = m_grid.numCells;
        m_grid.seqNum++;
        #endregion

        #region (re)allocate grid memory for the first time and if necessary
        bool cellArraysUnallocated = !m_cellStarts.IsCreated;
        bool cellArraysTooSmall = m_cellStarts.Length < m_grid.numCells;
        bool cellArraysTooBig = m_cellStarts.Length > m_grid.numCells * 2;
        // just too be sure, but it isn't nice because it could result in infrequent periodical stutter
        bool tooOld = m_grid.seqNum > 1000;

        if (cellArraysUnallocated || cellArraysTooSmall || cellArraysTooBig || tooOld)
        {
            m_cellStarts = new NativeArray<int>(m_grid.numCells, Allocator.Persistent);
            m_cellCapacities = new NativeArray<int>(m_grid.numCells, Allocator.Persistent);
            m_cellSizes = new NativeArray<int>(m_grid.numCells, Allocator.Persistent);
            m_cellSeqNums = new NativeArray<int>(m_grid.numCells, Allocator.Persistent);
            m_grid.seqNum = 1;
        }
        #endregion

        #region assign particles into grid, that is O(NumParticles)
        var gridJob = new FillGridJob();
        gridJob.translations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        int numParticles = gridJob.translations.Length;
        gridJob.cellContents = new NativeArray<int>(numParticles, Allocator.TempJob);
        gridJob.cellNumbers = new NativeArray<int>(numParticles, Allocator.TempJob);

        gridJob.grid = m_grid;


        gridJob.cellStarts = m_cellStarts;
        gridJob.cellCapacities = m_cellCapacities;
        gridJob.cellSizes = m_cellSizes;
        gridJob.cellSeqNums = m_cellSeqNums;


        gridJob.cellNeighborhoodContents = new NativeArray<int>(27 * numParticles, Allocator.TempJob);
        gridJob.cellNeighborhoodSizes = new NativeArray<int>(numParticles, Allocator.TempJob);

        gridJob.stats = new NativeArray<GridStats>(1, Allocator.TempJob);

        var gridJobHandle = gridJob.Schedule(inputDependencies);
        gridJobHandle.Complete();
        gridJob.translations.Dispose();
        #endregion

        #region particle life job, that is O(NumParticles*maxCellSize)
        var job = new ParticleLifeSystemJobFor();
        job.eps = 1e-6f;
        float scaledRealtime = particleLife.settings.simulationSpeedMultiplicator2 * UnityEngine.Time.deltaTime;
        if (abs(particleLife.settings.minSimulationStepRate) > job.eps)
        {
            job.dt = particleLife.settings.simulationSpeedMultiplicator * min(1.0f / abs(particleLife.settings.minSimulationStepRate), scaledRealtime);
        }
        else
        {
            job.dt = particleLife.settings.simulationSpeedMultiplicator * scaledRealtime;
        }
        
        lastSimulationDeltaTime = job.dt;
        job.r_smooth = particleLife.settings.r_smooth;
        job.flatForce = particleLife.settings.flatForce;
        job.friction = particleLife.settings.friction;
        job.frictionTime = particleLife.settings.frictionTime;
        job.strength = particleLife.settings.interactionStrength;
        job.maxSpeed = particleLife.settings.maxSpeed;


        job.numTypes = m_copyOfNumTypes;
        job.Attract = m_copyOfAttract;
        job.RangeMin = m_copyOfRangeMin;
        job.RangeMax = m_copyOfRangeMax;

        job.lowerBound = particleLife.settings.lowerBound;
        job.upperBound = particleLife.settings.upperBound;
        job.dim = job.upperBound - job.lowerBound;
        job.halfDim = job.dim * 0.5f;
        job.wrapX = particleLife.settings.wrapX;
        job.wrapY = particleLife.settings.wrapY;
        job.wrapZ = particleLife.settings.wrapZ;
        job.bounce = particleLife.settings.bounce;

        job.gravityTarget = particleLife.settings.gravityTarget * job.dim + job.lowerBound;
        job.gravityTargetRange = particleLife.settings.gravityTargetRange;
        job.gravityLinear = particleLife.settings.gravityLinear;
        job.gravityStrength = particleLife.settings.gravityStrength;
        job.maxGravity = particleLife.settings.maxGravity;

        job.oldTranslations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        job.oldParticles = m_query.ToComponentDataArray<Particle>(Allocator.TempJob);
        job.translations = m_query.ToComponentDataArray<Translation>(Allocator.TempJob);
        job.particles = m_query.ToComponentDataArray<Particle>(Allocator.TempJob);

        job.grid = gridJob.grid;
        job.cellNumbers = gridJob.cellNumbers;
        job.cellContents = gridJob.cellContents;
        job.cellStarts = gridJob.cellStarts;
        job.cellSizes = gridJob.cellSizes;
        job.cellSeqNums = gridJob.cellSeqNums;
        job.cellNeighborhoodContents = gridJob.cellNeighborhoodContents;
        job.cellNeighborhoodSizes = gridJob.cellNeighborhoodSizes;


        job.cellContents = gridJob.cellContents;
        job.cellStarts = gridJob.cellStarts;
        job.cellSizes = gridJob.cellSizes;

        job.numInteractionCandidates = new NativeArray<int>(1, Allocator.TempJob);
        job.numActualInteractions = new NativeArray<int>(1, Allocator.TempJob);


        // Now that the job is set up, schedule it to be run. 
        var jobHandle = job.Schedule(job.particles.Length, 16, inputDependencies);
        jobHandle.Complete();

        #endregion

        m_query.CopyFromComponentDataArray<Translation>(job.translations);
        m_query.CopyFromComponentDataArray<Particle>(job.particles);

        #region collect stats
        maxCellSize = gridJob.stats[0].maxCellSize;
        numOccupiedCells = gridJob.stats[0].numOccupiedCells;
        averageCellSize = gridJob.stats[0].averageCellSize;
        numCellAccesses = gridJob.stats[0].numCellAccesses;
        numInteractionCandidates = job.numInteractionCandidates[0];
        numActualInteractions = job.numActualInteractions[0];
        #endregion

        #region cleanup
        gridJob.stats.Dispose();
        job.numInteractionCandidates.Dispose();
        job.numActualInteractions.Dispose();

        job.oldTranslations.Dispose();
        job.oldParticles.Dispose();
        job.translations.Dispose();
        job.particles.Dispose();

        gridJob.cellNumbers.Dispose();
        gridJob.cellContents.Dispose();
        gridJob.cellNeighborhoodContents.Dispose();
        gridJob.cellNeighborhoodSizes.Dispose();
        #endregion
                     
        return inputDependencies;
    }
}