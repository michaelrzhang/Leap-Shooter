import java.io.IOException;
import java.lang.Math;
import com.leapmotion.leap.*;
import com.leapmotion.leap.Vector;
import com.leapmotion.leap.Gesture.State;
public class Block {
	boolean isBlock = false;
	public Block(Controller controller) {
		Frame frame = controller.frame();
		HandList hands = frame.hands();
		if (hands.count() < 2) {
			isBlock = false;
		} else if (hands.leftmost().grabStrength() < 0.4 && hands.rightmost().grabStrength() < 0.4) {
			isBlock = true;
		}
	}

	public boolean isBlock() {
		return isBlock;
	}
}