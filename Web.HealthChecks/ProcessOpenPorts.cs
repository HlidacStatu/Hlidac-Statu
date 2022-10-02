﻿
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ProcessOpenPorts : IHealthCheck
    {


        static IPNetwork[] radWareIPs = new string[] {
            "141.226.101.0/24",
            "66.22.0.0/17",
            "159.122.76.110",
            "141.226.97.0/24 "
            }
            .Select(i => IPNetwork.Parse(i))
            .ToArray();

        int warningThreshold = 4000;
        int errorThreshold = 8000;

        public ProcessOpenPorts()
        {
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            try
            {
                var currProcessPid = System.Diagnostics.Process.GetCurrentProcess().Id;

                int numInternalEstablished = 0;
                int numInternalTimeWait = 0;
                int numOtherEstablished = 0;
                int numOtherTimeWait = 0;

                foreach (TcpRow tcpRow in ManagedIpHelper.GetExtendedTcpTable(true)
                    .Where(m=>m.ProcessId == currProcessPid || m.ProcessId<=4)
                    )
                {
                    IPAddress remoteIp = tcpRow.RemoteEndPoint.Address;
                    if (radWareIPs.Any(i => i.Contains(remoteIp)) == false)
                    {
                        if (remoteIp.ToString().StartsWith("10.10.1"))
                        {
                            if (tcpRow.State == TcpState.Established)
                                numInternalEstablished++;
                            else
                                numInternalTimeWait++;
                        }
                        else
                        {
                            if (tcpRow.State == TcpState.Established)
                                numOtherEstablished++;
                            else
                                numOtherTimeWait++;
                        }
                    }

                }

                string report = $"Internal Established:{numInternalEstablished}<br/>\n"
                    + $"Internal Time_Wait:{numInternalTimeWait}<br/>\n"
                    + $"External Established:{numOtherEstablished}<br/>\n"
                    + $"External Time_Wait:{numOtherTimeWait}<br/>\n";

                if (numInternalEstablished+numInternalTimeWait+numOtherEstablished+numOtherTimeWait > errorThreshold)
                    return Task.FromResult(HealthCheckResult.Unhealthy(description: report));
                if (numInternalEstablished + numInternalTimeWait + numOtherEstablished + numOtherTimeWait > warningThreshold)
                    return Task.FromResult(HealthCheckResult.Degraded(description: report));
                else
                    return Task.FromResult(HealthCheckResult.Healthy(description: report));
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Cannot read data from system API", e));
            }


        }
    }

    #region Managed IP Helper API

    public class TcpTable : IEnumerable<TcpRow>
    {
        #region Private Fields

        private IEnumerable<TcpRow> tcpRows;

        #endregion

        #region Constructors

        public TcpTable(IEnumerable<TcpRow> tcpRows)
        {
            this.tcpRows = tcpRows;
        }

        #endregion

        #region Public Properties

        public IEnumerable<TcpRow> Rows
        {
            get { return this.tcpRows; }
        }

        #endregion

        #region IEnumerable<TcpRow> Members

        public IEnumerator<TcpRow> GetEnumerator()
        {
            return this.tcpRows.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.tcpRows.GetEnumerator();
        }

        #endregion
    }

    public class TcpRow
    {
        #region Private Fields

        private IPEndPoint localEndPoint;
        private IPEndPoint remoteEndPoint;
        private TcpState state;
        private int processId;

        #endregion

        #region Constructors

        public TcpRow(IpHelper.TcpRow tcpRow)
        {
            this.state = tcpRow.state;
            this.processId = tcpRow.owningPid;

            int localPort = (tcpRow.localPort1 << 8) + (tcpRow.localPort2) + (tcpRow.localPort3 << 24) + (tcpRow.localPort4 << 16);
            long localAddress = tcpRow.localAddr;
            this.localEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = (tcpRow.remotePort1 << 8) + (tcpRow.remotePort2) + (tcpRow.remotePort3 << 24) + (tcpRow.remotePort4 << 16);
            long remoteAddress = tcpRow.remoteAddr;
            this.remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }

        #endregion

        #region Public Properties

        public IPEndPoint LocalEndPoint
        {
            get { return this.localEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return this.remoteEndPoint; }
        }

        public TcpState State
        {
            get { return this.state; }
        }

        public int ProcessId
        {
            get { return this.processId; }
        }

        #endregion
    }

    public static class ManagedIpHelper
    {
        #region Public Methods

        public static TcpTable GetExtendedTcpTable(bool sorted)
        {
            List<TcpRow> tcpRows = new List<TcpRow>();

            IntPtr tcpTable = IntPtr.Zero;
            int tcpTableLength = 0;

            if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        IpHelper.TcpTable table = (IpHelper.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(IpHelper.TcpTable));

                        IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow((IpHelper.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(IpHelper.TcpRow))));
                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(IpHelper.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        #endregion
    }

    #endregion

    #region P/Invoke IP Helper API

    /// <summary>
    /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366073.aspx"/>
    /// </summary>
    public static class IpHelper
    {
        #region Public Fields

        public const string DllName = "iphlpapi.dll";
        public const int AfInet = 2;

        #endregion

        #region Public Methods

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
        /// </summary>
        [DllImport(IpHelper.DllName, SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

        #endregion

        #region Public Enums

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366386.aspx"/>
        /// </summary>
        public enum TcpTableType
        {
            BasicListener,
            BasicConnections,
            BasicAll,
            OwnerPidListener,
            OwnerPidConnections,
            OwnerPidAll,
            OwnerModuleListener,
            OwnerModuleConnections,
            OwnerModuleAll,
        }

        #endregion

        #region Public Structs

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpTable
        {
            public uint length;
            public TcpRow row;
        }

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpRow
        {
            public TcpState state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public int owningPid;
        }

        #endregion

        #endregion

    }
}


