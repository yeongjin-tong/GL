public interface ITimerControl
{
    // 현재 시간 값을 가져오는 함수
    float GetTime();

    // 시간을 설정하는 함수
    void SetTime(float newTime);
}