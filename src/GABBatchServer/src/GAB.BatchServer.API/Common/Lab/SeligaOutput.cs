namespace GAB.BatchServer.API.Common.Lab
{
    /// <summary>
    /// Represents a result of one Seliga run on the GAB 2017 Lab 
    /// </summary>
    
    // Lab output example before parsing:
    //#FUNOBS                   FUNTEST          FUNRESP                           X0           Y0          XS          YS          SFR0          ROT        VERTX     XNODE      XNODE      XNODE      XNODE      XNODE        YNODE       YNODE       YNODE       YNODE       YNODE        XSNODE      XSNODE      XSNODE      XSNODE      XSNODE      YSNODE      YSNODE      YSNODE      YSNODE      YSNODE    MSE        SCORE
    //FUNC/MOCKS/mockobs-sampleB000193.dat elpfn    RESPFUN/BRESP/sampleB-resp*.dat           2e+09       0.005       1e+08      0.0003         0.1           0           3     1.9e+09    1.95e+09       2e+09    2.05e+09     2.1e+09      0.0047     0.00485       0.005     0.00515      0.0053           0           1           0           1           0           1           1           1           1           1  0.00233661     94.5159

    public class SeligaOutput
    {
        /// <summary>
        /// Number of expected columns in a SELIGA file
        /// </summary>
        public const int SELIGA_COLUMNS = 32;
        /// <summary>
        /// Observed function
        /// </summary>
        public string FunObs { get; set; }
        /// <summary>
        /// Test function
        /// </summary>
        public string FunTest { get; set; }
        /// <summary>
        /// Response function
        /// </summary>
        public string FunResp { get; set; }
        /// <summary>
        /// Calculated score
        /// </summary>
        public double Score { get; set; }
    }
}
