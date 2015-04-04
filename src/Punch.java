//package samples;
import java.io.IOException;
import java.lang.Math;
import com.leapmotion.leap.*;
import com.leapmotion.leap.Vector;
import com.leapmotion.leap.Gesture.State;
public class Punch {
	boolean leftPunch = false;
	boolean rightPunch = false;
	public Punch(Controller controller) {
		Frame frame = controller.frame();
		HandList hands = frame.hands();
		for (Hand hand : hands) {
			Vector velocity = hand.palmVelocity();
			double zVelocity = velocity.getZ(); 
			if (hand.grabStrength() > 0.8 && zVelocity < -100) {
				if(hand.isLeft()) {
					leftPunch = true;
				} else {
					rightPunch = true;
				}
			}
		}
	}

	public boolean leftPunch() {
		return leftPunch;
	}
	public boolean rightPunch() {
		return rightPunch;
	}
}