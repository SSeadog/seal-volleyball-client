public class PlayerInputData {
  public int tick;
  public string sessionId;
  public bool left;
  public bool right;
  public bool jump;
  public bool receive;
  public bool toss;
  public bool spike;

  public override string ToString()
  {
    return $"[PlayerInputData] tick={tick}, sessionId={sessionId}, left={left}, right={right}, jump={jump}, receive={receive}, toss={toss}, spike={spike}";
  }
}