import java.io.*;
import java.lang.Math;
import com.leapmotion.leap.*;
import com.leapmotion.leap.Vector;
import com.leapmotion.leap.Gesture.State;
import jaco.mp3.player.MP3Player;
import java.io.File;

class SampleListener extends Listener {
    public boolean initialized = false;
    public boolean punchDelay = false;
    public boolean shootDelay = false;
    public long start;

    public void onInit(Controller controller) {
        System.out.println("Initialized");
    }

    // public void onConnect(Controller controller) {
    //     System.out.println("Connected");
    //     controller.enableGesture(Gesture.Type.TYPE_SWIPE);
    //     controller.enableGesture(Gesture.Type.TYPE_CIRCLE);
    //     controller.enableGesture(Gesture.Type.TYPE_SCREEN_TAP);
    //     controller.enableGesture(Gesture.Type.TYPE_KEY_TAP);
    // }

    public void onDisconnect(Controller controller) {
        //Note: not dispatched when running in a debugger.
        System.out.println("Disconnected");
    }

    public void onExit(Controller controller) {
        System.out.println("Exited");
    }

    public void onFrame(Controller controller) {
        // Get the most recent frame and report some basic information
        Frame frame = controller.frame();

        //Get hands
        for(Hand hand : frame.hands()) {

            // Get the hand's normal vector and direction
            // Vector normal = hand.palmNormal();
            // Vector direction = hand.direction();

            Vector velocity = hand.palmVelocity();
            double zVelocity = velocity.getZ(); 
            if (hand.grabStrength() > 0.8 && zVelocity < -500) {
                if (punchDelay == true && System.nanoTime() - start > 5*10e7) {
                    punchDelay = !punchDelay;
                }
                if (!punchDelay) {
                    System.out.println("punch");
                    start = System.nanoTime();
                    punchDelay = true;
                    try {
                        MP3Player punch = new MP3Player(new File("punch.mp3"));
                        punch.play();
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                }
            }

            // System.out.println("Normal: " + normal);
            double xVel = hand.palmVelocity().getX();
            // System.out.println("X Velocity: " + xVel);

            try {
                // File data = new File("data.txt");
                // FileWriter fw = new FileWriter(data.getAbsoluteFile());
                // BufferedWriter bw = new BufferedWriter(fw);
                 // bw.write(normal.toString());
                Test.pw.println("Normal: " + normal.toString());
            } catch (Exception e) {
                e.printStackTrace();
            }
           
            Vector thumb = null;
            Vector index = null;
            Vector middle = null;
            // Get fingers
            for (Finger finger : hand.fingers()) {
                // System.out.println("    " + finger.type() + ", direction: " + finger.direction());
                // Test.pw.println(finger.type() + ", direction: " + finger.direction());
                // System.out.println(finger.type().toString());
                if (finger.type().toString().equals("TYPE_THUMB")) {
                    thumb = finger.direction();
                }
                if (finger.type().toString().equals("TYPE_MIDDLE")) {
                    middle = finger.direction();
                    // System.out.println("Middle Finger Tip: " + finger.tipPosition());
                    // System.out.println("Middle Finger Direction: " + finger.direction());
                }
                if (finger.type().toString().equals("TYPE_INDEX")) {
                    index = finger.direction();
                }
            }

            if (xVel < -500 && RightAngleParser.parseRightAngle(thumb, index, middle)) {
                if (shootDelay == true && System.nanoTime() - start > 5 * 10e7) {
                    shootDelay = !shootDelay;
                }
                if (!shootDelay) {
                    System.out.println("shot");
                    start = System.nanoTime();
                    shootDelay = true;
                    try {
                        MP3Player gunShot = new MP3Player(new File("gun-gunshot-01.mp3"));
                        gunShot.play();
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                }
            }

                //Get Bones
                // for(Bone.Type boneType : Bone.Type.values()) {
                //     Bone bone = finger.bone(boneType);
                //     System.out.println("      " + bone.type()
                //                      + " bone, start: " + bone.prevJoint()
                //                      + ", end: " + bone.nextJoint()
                //                      + ", direction: " + bone.direction());
                // }
            
        

        // Get tools
        // for(Tool tool : frame.tools()) {
        //     System.out.println("  Tool id: " + tool.id()
        //                      + ", position: " + tool.tipPosition()
        //                      + ", direction: " + tool.direction());
        // }

        // if (!frame.hands().isEmpty()) {
        //     System.out.println();
        // }

        // GestureList gestures = frame.gestures();
        // for (int i = 0; i < gestures.count(); i++) {
        //     Gesture gesture = gestures.get(i);

        //     // switch (gesture.type()) {
        //         // case TYPE_CIRCLE:
        //         //     CircleGesture circle = new CircleGesture(gesture);

        //         //     // Calculate clock direction using the angle between circle normal and pointable
        //         //     String clockwiseness;
        //         //     if (circle.pointable().direction().angleTo(circle.normal()) <= Math.PI/2) {
        //         //         // Clockwise if angle is less than 90 degrees
        //         //         clockwiseness = "clockwise";
        //         //     } else {
        //         //         clockwiseness = "counterclockwise";
        //         //     }

        //         //     // Calculate angle swept since last frame
        //         //     double sweptAngle = 0;
        //         //     if (circle.state() != State.STATE_START) {
        //         //         CircleGesture previousUpdate = new CircleGesture(controller.frame(1).gesture(circle.id()));
        //         //         sweptAngle = (circle.progress() - previousUpdate.progress()) * 2 * Math.PI;
        //         //     }

        //         //     System.out.println("  Circle id: " + circle.id()
        //         //                + ", " + circle.state()
        //         //                + ", progress: " + circle.progress()
        //         //                + ", radius: " + circle.radius()
        //         //                + ", angle: " + Math.toDegrees(sweptAngle)
        //         //                + ", " + clockwiseness);
        //         //     break;
        //         // case TYPE_SWIPE:
        //         //     SwipeGesture swipe = new SwipeGesture(gesture);
        //         //     System.out.println("  Swipe id: " + swipe.id()
        //         //                + ", " + swipe.state()
        //         //                + ", position: " + swipe.position()
        //         //                + ", direction: " + swipe.direction()
        //         //                + ", speed: " + swipe.speed());
        //         //     break;
        //         // case TYPE_SCREEN_TAP:
        //         //     ScreenTapGesture screenTap = new ScreenTapGesture(gesture);
        //         //     System.out.println("  Screen Tap id: " + screenTap.id()
        //         //                + ", " + screenTap.state()
        //         //                + ", position: " + screenTap.position()
        //         //                + ", direction: " + screenTap.direction());
        //         //     break;
        //         // case TYPE_KEY_TAP:
        //         //     KeyTapGesture keyTap = new KeyTapGesture(gesture);
        //         //     System.out.println("  Key Tap id: " + keyTap.id()
        //         //                + ", " + keyTap.state()
        //         //                + ", position: " + keyTap.position()
        //         //                + ", direction: " + keyTap.direction());
        //         //     break;
        //         // default:
        //         //     System.out.println("Unknown gesture type.");
        //         //     break;
        //     }
        }
    }
}


public class Movements {
    public static PrintWriter pw;

    public static void main(String[] args) {
        // Create a sample listener and controller
        SampleListener listener = new SampleListener();
        Controller controller = new Controller();

        try {
             pw = new PrintWriter("data.txt");
            } catch (Exception e) {}

        // Have the sample listener receive events from the controller
        controller.addListener(listener);

        // Keep this process running until Enter is pressed
        System.out.println("Press Enter to quit...");
        try {
            System.in.read();
        } catch (IOException e) {
            e.printStackTrace();
        }

        // Remove the sample listener when done
        controller.removeListener(listener);
        pw.close();
    }
}