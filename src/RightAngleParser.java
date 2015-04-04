// x (float) – The x component. Corresponds to the long (left to right) dimension of the Leap Motion controller.
// y (float) – The y component. Corresponds to height above the device.
// z (float) – The z component. Corresponds to the short (front-back) dimension of the Leap Motion controller.


// Normal: (-0.250941, -0.967467, -0.0321888)
// TYPE_THUMB, direction: (-0.919814, 0.118805, -0.373936)
// TYPE_INDEX, direction: (0.0484073, -0.0685681, -0.996471)
// TYPE_MIDDLE, direction: (-0.0498719, -0.0495859, -0.997524)
// TYPE_RING, direction: (-0.42958, -0.705136, 0.564132)
// TYPE_PINKY, direction: (-0.718146, -0.55944, 0.413877)

// Normal: (-0.248183, -0.968184, -0.0320173)
// TYPE_THUMB, direction: (-0.91989, 0.116378, -0.374512)
// TYPE_INDEX, direction: (0.0475537, -0.0680149, -0.99655)
// TYPE_MIDDLE, direction: (-0.0508572, -0.0503352, -0.997437)
// TYPE_RING, direction: (-0.426816, -0.705519, 0.565747)
// TYPE_PINKY, direction: (-0.716161, -0.561534, 0.414479)


import com.leapmotion.leap.*;
import com.leapmotion.leap.Vector;
import com.leapmotion.leap.Gesture.State;
public class RightAngleParser {
    public static void main(String[] args) {
        Vector thumb = new Vector((float) -0.919814, (float) 0.118805, (float) -0.373936);
        Vector index = new Vector((float) 0.0484073, (float) -0.0685681, (float) -0.996471);
        parseRightAngle(thumb, index);
    }
    public static boolean parseRightAngle(Vector first, Vector second) {
        float dot = first.dot(second);
        System.out.println(dot);
        return (dot < 0.4);
    }

    /** Helper function that returns the dot product of two vectors */
    public static float dot(Vector first, Vector second) {
        return first.dot(second);
    }

}