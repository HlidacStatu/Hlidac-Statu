using System;

namespace AsrRunner;

public class FailSaveCounter
{
    private int _consecutiveFails = 0;
    private int _failTreshold;

    public FailSaveCounter(int failTreshold)
    {
        _failTreshold = failTreshold;
    }

    public void ResetCounter() => _consecutiveFails = 0;

    public void ReportFail()
    {
        if (_consecutiveFails++ >= _failTreshold)
            throw new FailTresholdOverflow(_consecutiveFails, _failTreshold);
    }
    
    public class FailTresholdOverflow : Exception
    {
        public FailTresholdOverflow(int consecutiveFails, int treshold)
            : base($"{consecutiveFails} consecutive fails exceeded treshold of {treshold}")
        { }
    }

    
}