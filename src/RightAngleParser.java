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
import java.io.IOException;
import java.io.BufferedReader;
import java.io.FileReader;
import java.io.File;

public class RightAngleParser {
    public static void main(String[] args) {
        BufferedReader reader = null;
        // dotProductTest();
        try {
            File file = new File("data.txt");
            reader = new BufferedReader(new FileReader(file));
            String line;
            String[] parsed;
            float x, y, z;
            Vector thumb = null;
            Vector index = null;
            Vector middle = null;
            int total = 0;
            int success = 0;
            while ((line = reader.readLine()) != null) {
                if (line.contains("THUMB")) {
                    // System.out.println(line);
                    parsed = line.split(",");
                    /** Debugging */
                    // System.out.println(parsed[1].substring(13));
                    // System.out.println(parsed[2].substring(1));
                    // System.out.println(parsed[3].substring(1));
                    x = Float.parseFloat(parsed[1].substring(13));
                    y = Float.parseFloat(parsed[2].substring(1));
                    z = Float.parseFloat(parsed[3].substring(1, parsed[3].length() - 1));
                    /** Debugging */
                    // System.out.println(x);
                    // System.out.println(y);
                    // System.out.println(z);
                    thumb = new Vector(x, y , z);
                    System.out.println(thumb);
                } else if (line.contains("INDEX")) {
                    // System.out.println(line);
                    parsed = line.split(",");
                    x = Float.parseFloat(parsed[1].substring(13));
                    y = Float.parseFloat(parsed[2].substring(1));
                    z = Float.parseFloat(parsed[3].substring(1, parsed[3].length() - 1));
                    index = new Vector(x, y , z);
                    System.out.println(index);
                } else if (line.contains("MIDDLE")) {
                    // System.out.println(line);
                    parsed = line.split(",");
                    x = Float.parseFloat(parsed[1].substring(13));
                    y = Float.parseFloat(parsed[2].substring(1));
                    z = Float.parseFloat(parsed[3].substring(1, parsed[3].length() - 1));
                    middle = new Vector(x, y , z);
                    // System.out.println(middle);
                    // System.out.println(parseRightAngle(thumb, index, middle));
                    boolean right = parseRightAngle(thumb, index, middle);
                    if (right) {
                        success += 1;
                        total += 1;
                    } else {
                        total += 1;
                    }
                    System.out.println((double) success / total);
                }   
            }
        } catch (IOException e) {
            e.printStackTrace();
        } finally {
            try {
                reader.close();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        // Vector thumb = new Vector((float) -0.919814, (float) 0.118805, (float) -0.373936);
        // Vector index = new Vector((float) 0.0484073, (float) -0.0685681, (float) -0.996471);
        // parseRightAngle(thumb, index);
    }

    /** Returns whether two vectors form a right angle */
    public static boolean parseRightAngle(Vector thumb, Vector index, Vector middle) {
        float dot = thumb.dot(middle);
        // System.out.println(dot);
        // System.out.println("====New test!====");
        // if (Math.abs((Math.acos(dot) - Math.PI/2)) < 0.48) {
        //     System.out.println("Pass 1!");
        // }
        // if (Math.abs(Math.acos(thumb.dot(index)) - Math.PI/2) < 0.48) {
        //     System.out.println("Pass 2!");
        // }
        // if (index.dot(middle) > 0.90) {
        //     System.out.println("Pass 3!");
        // }
        return (Math.abs((Math.acos(dot) - Math.PI/2)) < 0.55) && 
        (Math.abs((Math.acos(thumb.dot(index)) - Math.PI/2)) < 0.55) &&
        index.dot(middle) > 0.90;
    }

    /** Helper function that returns the dot product of two vectors */
    public static float dot(Vector first, Vector second) {
        return first.dot(second);
    }

    /** Some experiments to see what angle is good */
    // public static void dotProductTest() {
    //     float a, b, c;
    //     a = 1;
    //     b = 0;
    //     c = 0;
    //     Vector first = new Vector(a, b, c);
    //     a = (float) 0.6;
    //     b = (float) 0.8;
    //     c = 0;
    //     Vector second = new Vector(a, b, c);
    //     a = (float) -0.6;
    //     b = (float) 0.8;
    //     c = 0;
    //     Vector third = new Vector(a, b, c);
    //     a = (float) -0.6;
    //     b = (float) -0.8;
    //     c = 0;
    //     Vector fourth = new Vector(a, b, c);
    //     System.out.println(dot(first, second));
    //     System.out.println(dot(first, third));
    //     System.out.println(Math.acos(dot(first, second)));
    //     System.out.println(Math.PI - Math.acos(dot(first, third)));
    //     System.out.println(Math.PI - Math.acos(dot(first, fourth)));
    // }

}