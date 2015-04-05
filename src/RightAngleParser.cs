using System;

class RightAngleParser {
    public static bool parseRightAngle (Vector thumb, Vector index, Vector middle) {
        return (Math.Abs((Math.Acos(thumb.Dot(index)) - Math.PI/2)) < 0.70) & 
        (Math.Abs((Math.Acos(thumb.Dot(middle)) - Math.PI/2)) < 0.70) &
        index.Dot(middle) > 0.90;
    }
}