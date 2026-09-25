using System;
using System.Threading;

namespace Boids.Art.Infinite
{
    /// <summary>Pure, deterministic tiled WFC. No Unity objects or global random state.</summary>
    public static class SkyCityWfc
    {
        public const int Size = 8, CellCount = Size * Size, ModuleCount = 128;
        public const int StateCount = ModuleCount * 4 + 1, Empty = StateCount - 1;
        public const float TileSize = 12, ChunkSize = 168;
        public static readonly int[] FamilyMasks = { 5, 3, 11, 15, 1, 5, 1, 3, 5, 11, 5, 3, 5, 1, 11, 15 };
        static readonly double[] FamilyWeights = { 1.6, 1.0, .7, .9, 1.2, .65, .33, 1.3, 1.0, .65, .25, .2, .5, .25, .55, .26 };
        static readonly int[] DX = { 0, 1, 0, -1 }, DZ = { 1, 0, -1, 0 };
        static readonly int[] Masks = CreateMasks();
        static readonly bool[] Tall = CreateTall();

        public struct Coord : IEquatable<Coord>
        {
            public long x, z;
            public Coord(long x, long z) { this.x = x; this.z = z; }
            public bool Equals(Coord other) { return x == other.x && z == other.z; }
            public override bool Equals(object obj) { return obj is Coord && Equals((Coord)obj); }
            public override int GetHashCode() { unchecked { return (int)Hash(x, z, 471); } }
            public override string ToString() { return x + "," + z; }
        }
        public sealed class Result
        {
            public Coord coord;
            public int[] states;
            public int observations, propagations, restarts, composition, seed;
            public bool usedSafeFallback;
            public ulong fingerprint;
        }
        public static ulong Hash(long x, long z, int seed)
        {
            unchecked
            {
                ulong h = (ulong)x * 0x9E3779B185EBCA87UL ^ (ulong)z * 0xC2B2AE3D27D4EB4FUL ^ (uint)seed;
                h ^= h >> 30; h *= 0xBF58476D1CE4E5B9UL; h ^= h >> 27; h *= 0x94D049BB133111EBUL;
                return h ^ (h >> 31);
            }
        }
        struct Random
        {
            ulong value;
            public Random(ulong seed) { value = seed == 0 ? 1 : seed; }
            public double Next() { value ^= value << 13; value ^= value >> 7; value ^= value << 17; return (value >> 11) * (1.0 / 9007199254740992.0); }
        }
        static int[] CreateMasks()
        {
            var a = new int[StateCount];
            for (int s = 0; s < Empty; s++) { int m = FamilyMasks[s / 32], r = s % 4; a[s] = ((m << r) | (m >> (4 - r))) & 15; }
            return a;
        }
        static bool[] CreateTall()
        {
            var a = new bool[StateCount];
            for (int s = 0; s < Empty; s++) { int family = s / 32; a[s] = family == 6 || family == 13 || family == 15; }
            return a;
        }
        public static int Socket(int state, int direction) { return (Masks[state] >> direction) & 1; }
        public static bool Compatible(int a, int b, int direction)
        { return Socket(a, direction) == Socket(b, (direction + 2) % 4) && !(Tall[a] && Tall[b]); }

        // Both sides derive a shared boundary from the SAME canonical edge key.
        public static int Gate(Coord c, int direction, int seed)
        {
            long x = c.x, z = c.z;
            if (direction == 0) z++;
            if (direction == 1) x++;
            // Two approach bridges frame the opening palace; their reverse edges
            // use these same canonical keys. Elsewhere the cloud gaps stay open.
            if(x==0&&z==0)return -1;
            if(x==1&&z==0&&(direction&1)==1)return 4;
            if(x==0&&z==1&&(direction&1)==0)return 3;
            ulong h = Hash(x, z, seed ^ ((direction & 1) == 0 ? 907 : 1483));
            return h % 5 < 3 ? -1 : 3 + (int)((h >> 9) & 1);
        }
        public static bool Land(Coord c, int x, int z, int seed)
        {
            if (z == 0) return x == Gate(c, 2, seed);
            if (z == Size - 1) return x == Gate(c, 0, seed);
            if (x == 0) return z == Gate(c, 3, seed);
            if (x == Size - 1) return z == Gate(c, 1, seed);
            if ((x == 1 || x == Size - 2) && (z == 1 || z == Size - 2)) return false;
            // The authored sanctuary has open ground-level routes on all sides.
            // Its immediate garden ring must exist before WFC can propagate sockets.
            if (x == 1 && z >= 3 && z <= 5 || z == 6 && x >= 2 && x <= 4 || x == 3 && z == 1) return true;
            // Variation in the shoreline never removes the interior end of a gateway.
            if (x == 1 && z == Gate(c,3,seed) || x == Size-2 && z == Gate(c,1,seed) ||
                z == 1 && x == Gate(c,2,seed) || z == Size-2 && x == Gate(c,0,seed)) return true;
            if (x == 1 || x == Size-2 || z == 1 || z == Size-2)
                return Hash(c.x*8+x,c.z*8+z,seed^173) % 5 != 0;
            return true;
        }
        public static int Composition(Coord coord,int seed)
        {
            if(coord.x==0&&coord.z==0)return 0;
            int parity=(int)(coord.x&1)+2*(int)(coord.z&1);
            return parity+4*(int)(Hash(coord.x,coord.z,seed^2789)&1);
        }
        public static float Elevation(int composition)
        {
            switch(composition){case 1:return 34;case 2:return 8;case 3:return 44;case 4:return 60;case 5:return 11;case 6:return 25;case 7:return 5;default:return 18;}
        }

        public static Result Solve(Coord coord, int seed, CancellationToken cancellation)
        {
            // Four disjoint silhouette pairs: no touching (even diagonal) districts
            // repeat the same landmark. The hash picks between the pair's two forms.
            var result = new Result { coord = coord, composition = Composition(coord,seed), seed=seed };
            var allowed = new bool[CellCount * StateCount];
            var counts = new int[CellCount]; var queue = new int[CellCount]; var queued = new bool[CellCount];
            var weights = new double[StateCount];
            var weightLogs = new double[StateCount];
            int preferredVariant = (int)(Hash((long)Math.Floor(coord.x / 3.0), (long)Math.Floor(coord.z / 3.0), seed) % 8);
            for (int s = 0; s < Empty; s++)
            {
                int family=s/32;double districtWeight=1;
                if(result.composition==2||result.composition==5)districtWeight=family==3||family==7||family==8?3.5:.20;
                if(result.composition==6)districtWeight=family==2||family==9||family==14?3:.4;
                if(result.composition==7)districtWeight=family==0||family==1||family==11||family==12?3:.25;
                weights[s] = FamilyWeights[family]*districtWeight*((s / 4 % 8) == preferredVariant ? 1.6 : 1);
            }
            weights[Empty] = 1;
            for (int s = 0; s < StateCount; s++) weightLogs[s] = weights[s] * Math.Log(weights[s]);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                cancellation.ThrowIfCancellationRequested(); result.restarts = attempt;
                Array.Clear(allowed, 0, allowed.Length); Array.Clear(counts, 0, counts.Length); Array.Clear(queued, 0, queued.Length);
                var random = new Random(Hash(coord.x, coord.z, seed ^ attempt * 31337));
                int head = 0, tail = 0, pending = 0;
                for (int cell = 0; cell < CellCount; cell++)
                {
                    int x = cell % Size, z = cell / Size; bool land = Land(coord, x, z, seed);
                    bool edge = x == 0 || z == 0 || x == Size - 1 || z == Size - 1;
                    for (int s = 0; s < StateCount; s++)
                    {
                        bool valid = land ? s != Empty : s == Empty;
                        if (land && edge) valid &= s / 32 == 10 && (s/4%8==0||s/4%8==2) && Masks[s] == (x == 0 || x == Size - 1 ? 10 : 5);
                        if (land && !edge && s != Empty)
                        {
                            int family = s / 32;
                            // One designed sanctuary, ringed by subordinate streets and gardens.
                            // The sanctuary occupies open piazza sockets and connects at ground level.
                            if (Sanctuary(cell)) valid &= family == 3;
                            else if (family == 6 || family == 13 || family == 15)
                                valid &= (x == 1 && z == 4) || (x == 5 && z == 5);
                        }
                        if (valid)
                        {
                            for (int d = 0; d < 4; d++)
                            {
                                int nx = x + DX[d], nz = z + DZ[d];
                                if (nx < 0 || nx >= Size || nz < 0 || nz >= Size)
                                    valid &= Socket(s, d) == (land ? 1 : 0);
                            }
                        }
                        if (valid) { allowed[cell * StateCount + s] = true; counts[cell]++; }
                    }
                    queue[tail] = cell; tail = (tail + 1) % CellCount; pending++; queued[cell] = true;
                }
                bool contradiction = false;
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();
                    while (pending > 0 && !contradiction)
                    {
                        int cell = queue[head]; head = (head + 1) % CellCount; pending--; queued[cell] = false;
                        if (counts[cell] == 0) { contradiction = true; break; }
                        int x = cell % Size, z = cell / Size;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + DX[d], nz = z + DZ[d]; if (nx < 0 || nx >= Size || nz < 0 || nz >= Size) continue;
                            int neighbor = nz * Size + nx, supported = 0, supportedByShort = 0;
                            for (int s = 0; s < StateCount; s++) if (allowed[cell * StateCount + s])
                            { int bit = 1 << Socket(s, d); supported |= bit; if (!Tall[s]) supportedByShort |= bit; }
                            bool changed = false;
                            for (int s = 0; s < StateCount; s++)
                            {
                                int index = neighbor * StateCount + s;
                                if (!allowed[index]) continue;
                                int bit = 1 << Socket(s, (d + 2) % 4);
                                if (((Tall[s] ? supportedByShort : supported) & bit) == 0)
                                { allowed[index] = false; counts[neighbor]--; changed = true; result.propagations++; }
                            }
                            if (counts[neighbor] == 0) { contradiction = true; break; }
                            if (changed && !queued[neighbor]) { queued[neighbor] = true; queue[tail] = neighbor; tail = (tail + 1) % CellCount; pending++; }
                        }
                    }
                    if (contradiction) break;
                    int chosen = -1; double minimumEntropy = double.MaxValue;
                    for (int cell = 0; cell < CellCount; cell++)
                    {
                        if (counts[cell] <= 1) continue;
                        double sum = 0, logSum = 0;
                        for (int s = 0; s < StateCount; s++) if (allowed[cell * StateCount + s])
                        { sum += weights[s]; logSum += weightLogs[s]; }
                        double entropy = Math.Log(sum) - logSum / sum + random.Next() * 1e-6;
                        if (entropy < minimumEntropy) { chosen = cell; minimumEntropy = entropy; }
                    }
                    if (chosen < 0)
                    {
                        result.states = new int[CellCount];
                        for (int cell = 0; cell < CellCount; cell++)
                            for (int s = 0; s < StateCount; s++) if (allowed[cell * StateCount + s]) { result.states[cell] = s; break; }
                        Fingerprint(result); return result;
                    }
                    double total = 0;
                    for (int s = 0; s < StateCount; s++) if (allowed[chosen * StateCount + s]) total += weights[s];
                    double roll = random.Next() * total; int selected = -1;
                    for (int s = 0; s < StateCount; s++) if (allowed[chosen * StateCount + s])
                    { selected = s; roll -= weights[s]; if (roll <= 0) break; }
                    for (int s = 0; s < StateCount; s++) allowed[chosen * StateCount + s] = s == selected;
                    counts[chosen] = 1; queue[tail] = chosen; tail = (tail + 1) % CellCount; pending++; queued[chosen] = true;
                    result.observations++;
                }
            }
            // Bounded recovery: connect every land neighbor, choose non-tall exact sockets.
            // This is a validated layout, never an invalid unconstrained random fill.
            result.usedSafeFallback = true; result.states = new int[CellCount];
            for (int cell = 0; cell < CellCount; cell++)
            {
                cancellation.ThrowIfCancellationRequested(); int x = cell % Size, z = cell / Size;
                if (!Land(coord, x, z, seed)) { result.states[cell] = Empty; continue; }
                int mask = 0;
                for (int d = 0; d < 4; d++)
                { int nx = x + DX[d], nz = z + DZ[d]; if (nx < 0 || nx >= Size || nz < 0 || nz >= Size || Land(coord, nx, nz, seed)) mask |= 1 << d; }
                bool found = false;
                for (int s = 0; s < Empty; s++) if (Masks[s] == mask && !Tall[s]) { result.states[cell] = s; found = true; break; }
                if (!found) throw new InvalidOperationException("No safe socket template for " + mask);
            }
            Fingerprint(result); return result;
        }
        static void Fingerprint(Result result)
        {
            ulong h = 1469598103934665603UL;
            unchecked { foreach (int state in result.states) { h ^= (uint)state; h *= 1099511628211UL; } }
            result.fingerprint = h;
        }
        public static bool Sanctuary(int cell)
        {
            int x = cell % Size, z = cell / Size;
            return (x >= 2 && x <= 4 && z >= 3 && z <= 5) || (x == 3 && z == 2);
        }
        // This authored waterway replaces a parcel's visible building without
        // forcing four open road sockets against a possibly closed island edge.
        public static bool Waterway(int cell) { return cell == 11; }
    }
}
