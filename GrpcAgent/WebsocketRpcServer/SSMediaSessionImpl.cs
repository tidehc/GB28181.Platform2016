﻿using Grpc.Core;
using MediaContract;
using System.Threading.Tasks;
using SIPSorcery.GB28181.Servers;
using System;

namespace GrpcAgent.WebsocketRpcServer
{
    public class SSMediaSessionImpl : VideoSession.VideoSessionBase
    {

        private MediaEventSource _eventSource = null;
        private ISIPServiceDirector _sipServiceDirector = null;

        public SSMediaSessionImpl(MediaEventSource eventSource, ISIPServiceDirector sipServiceDirector)
        {
            _eventSource = eventSource;
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            var keepAliveReply = new KeepAliveReply()
            {
                Status = new MediaContract.Status()
                {
                    Code = 200,
                    Msg = "KeepAlive Successful!"
                }
            };
            return Task.FromResult(keepAliveReply);
        }

        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {
            try
            {
                _eventSource?.FireLivePlayRequestEvent(request, context);
                var reqeustProcessResult = _sipServiceDirector.MakeVideoRequest(request.Gbid, new int[] { request.Port }, request.Ipaddr);

                reqeustProcessResult?.Wait(System.TimeSpan.FromSeconds(1));

                //get the response .
                var resReply = new StartLiveReply()
                {
                    Ipaddr = reqeustProcessResult.Result.Item1,
                    Port = reqeustProcessResult.Result.Item2,
                    Hdr = request.Hdr,

                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = "Request Successful!"
                    }

                };
                return Task.FromResult(resReply);
            }
            catch(Exception ex)
            {
                var resReply = new StartLiveReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(resReply);
            }
        }

        public override Task<StartPlaybackReply> StartPlayback(StartPlaybackRequest request, ServerCallContext context)
        {
            if (request.IsDownload)
            {
                _eventSource?.FireDownloadRequestEvent(request, context);
            }
            else
            {
                _eventSource?.FirePlaybackRequestEvent(request, context);
            }

            return base.StartPlayback(request, context);
        }

        public override Task<StopReply> Stop(StopRequest request, ServerCallContext context)
        {
            try
            {
                var stopProcessResult = _sipServiceDirector.Stop(string.IsNullOrEmpty(request.Gbid) ? "42010000001310000184" : request.Gbid);
                var stopReply = new StopReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = "Stop Successful!"
                    }
                };
                return Task.FromResult(stopReply);
            }
            catch (Exception ex)
            {
                var stopReply = new StopReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(stopReply);
            }
        }
    }
}
