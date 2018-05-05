using System;
using UnityEngine;

[Serializable]
public struct Vector3i {
    // Fields
    public int x;

    public int y;

    public int z;


    // Indexer 
    public int this[int index] {
        get {
            switch (index) {
                case X_INDEX:
                    return this.x;
                case Y_INDEX:
                    return this.y;
                case Z_INDEX:
                    return this.z;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3i index!");
            }
        }
        set {
            switch (index) {
                case X_INDEX:
                    this.x = value;
                    break;
                case Y_INDEX:
                    this.y = value;
                    break;
                case Z_INDEX:
                    this.z = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Vector3i index!");
            }
        }
    }

    // Constructors
    public Vector3i(int x, int y) {
        this.x = x;
        this.y = y;
        this.z = 0;
    }

    public Vector3i(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    // Properties
    public float sqrMagnitude {
        get { return x * x + y * y + z * z; }
    }

    public float magnitude {
        get { return Mathf.Sqrt(sqrMagnitude); }
    }

    public bool IsWithinBounds(Vector3i from, Vector3i to) {
        return this.x >= from.x && this.x < to.x &&
               this.y >= from.y && this.y < to.y &&
               this.z >= from.z && this.z < to.z;
    }

    // Set
    public void Set(int new_x, int new_y, int new_z) {
        this.x = new_x;
        this.y = new_y;
        this.z = new_z;
    }

    // Scaling
    public void Scale(Vector3i scale) {
        x *= scale.x;
        y *= scale.y;
        z *= scale.z;
    }

    public static Vector3i Scale(Vector3i a, Vector3i b) {
        return new Vector3i(
            a.x * b.x,
            a.y * b.y,
            a.z * b.z
        );
    }

    // Rotations

    public void RotateCW(int axis) {
        int temp;
        switch (axis) {
            case 0:
                temp = y;
                y = -z;
                z = temp;
                break;
            case 1:
                temp = x;
                x = z;
                z = -temp;
                break;
            case 2:
                temp = x;
                x = -y;
                y = temp;
                break;
        }
    }

    public void RotateCCW(int axis) {
        int temp;
        switch (axis) {
            case 0:
                temp = y;
                y = z;
                z = -temp;
                break;
            case 1:
                temp = x;
                x = -z;
                z = temp;
                break;
            case 2:
                temp = x;
                x = y;
                y = -temp;
                break;
        }
    }

    // Loops
    public enum LoopOrder : int {
        ZYX,
        ZXY,
        XZY,
        YZX,
        YXZ,
        XYZ
    }

    private static int[,] loopCoords = new int[,]{
			{2,1,0},
			{2,0,1},
			{0,2,1},
			{1,2,0},
			{1,0,2},
			{0,1,2}
		};

    private static int GetCoord(LoopOrder loopOrder, int loopLevel) {
        return loopCoords[(int)loopOrder, loopLevel];
    }

    public static void CubeLoop(Vector3i from, Vector3i to, Action<Vector3i> body) {
        if (body == null) {
            throw new ArgumentNullException("body");
        }

        Vector3i iterator = Vector3i.zero;
        for (iterator.x = from.x; iterator.x < to.x; iterator.x++) {
            for (iterator.y = from.y; iterator.y < to.y; iterator.y++) {
                for (iterator.z = from.z; iterator.z < to.z; iterator.z++) {
                    body(iterator);
                }
            }
        }
    }

    // ToString
    public override string ToString() {
        return string.Format("({0}, {1}, {2})", x, y, z);
    }

    // Operators
    public static Vector3i operator +(Vector3i a, Vector3i b) {
        return new Vector3i(
            a.x + b.x,
            a.y + b.y,
            a.z + b.z
        );
    }

    public static Vector3i operator -(Vector3i a) {
        return new Vector3i(
            -a.x,
            -a.y,
            -a.z
        );
    }

    public static Vector3i operator -(Vector3i a, Vector3i b) {
        return a + (-b);
    }

    public static Vector3i operator *(int d, Vector3i a) {
        return new Vector3i(
            d * a.x,
            d * a.y,
            d * a.z
        );
    }

    public static Vector3i operator *(Vector3i a, int d) {
        return d * a;
    }

    public static Vector3i operator /(Vector3i a, int d) {
        return new Vector3i(
            a.x / d,
            a.y / d,
            a.z / d
        );
    }

    // Equality
    public static bool operator ==(Vector3i lhs, Vector3i rhs) {
        return lhs.x == rhs.x &&
               lhs.y == rhs.y &&
               lhs.z == rhs.z;
    }

    public static bool operator !=(Vector3i lhs, Vector3i rhs) {
        return !(lhs == rhs);
    }

    public override bool Equals(object other) {
        if (!(other is Vector3i)) {
            return false;
        }
        return this == (Vector3i)other;
    }

    public bool Equals(Vector3i other) {
        return this == other;
    }

    public override int GetHashCode() {
        return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
    }

    // Static Methods

    public static float Distance(Vector3i a, Vector3i b) {
        return (a - b).magnitude;
    }

    public static Vector3i Min(Vector3i lhs, Vector3i rhs) {
        return new Vector3i(
            Mathf.Min(lhs.x, rhs.x),
            Mathf.Min(lhs.y, rhs.y),
            Mathf.Min(lhs.z, rhs.z)
        );
    }

    public static Vector3i Max(Vector3i lhs, Vector3i rhs) {
        return new Vector3i(
            Mathf.Max(lhs.x, rhs.x),
            Mathf.Max(lhs.y, rhs.y),
            Mathf.Max(lhs.z, rhs.z)
        );
    }

    public static int Dot(Vector3i lhs, Vector3i rhs) {
        return lhs.x * rhs.x +
               lhs.y * rhs.y +
               lhs.z * rhs.z;
    }

    public static Vector3i Cross(Vector3i lhs, Vector3i rhs) {
        return new Vector3i(
            lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.x * rhs.y - lhs.y * rhs.x
        );
    }

    public static float Magnitude(Vector3i a) {
        return a.magnitude;
    }

    public static float SqrMagnitude(Vector3i a) {
        return a.sqrMagnitude;
    }

    // Default values
    public static Vector3i back {
        get { return new Vector3i(0, 0, -1); }
    }

    public static Vector3i forward {
        get { return new Vector3i(0, 0, 1); }
    }

    public static Vector3i down {
        get { return new Vector3i(0, -1, 0); }
    }

    public static Vector3i up {
        get { return new Vector3i(0, +1, 0); }
    }

    public static Vector3i left {
        get { return new Vector3i(-1, 0, 0); }
    }

    public static Vector3i right {
        get { return new Vector3i(+1, 0, 0); }
    }

    public static Vector3i one {
        get { return new Vector3i(+1, +1, +1); }
    }

    public static Vector3i zero {
        get { return new Vector3i(0, 0, 0); }
    }

    // Conversions
    public static explicit operator Vector3i(Vector3 source) {
        return new Vector3i((int)source.x, (int)source.y, (int)source.z);
    }

    public static implicit operator Vector3(Vector3i source) {
        return new Vector3(source.x, source.y, source.z);
    }

    // Constants
    public const int X_INDEX = 0;
    public const int Y_INDEX = 1;
    public const int Z_INDEX = 2;
}