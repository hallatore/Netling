using System;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core;
using Netling.Core.Models;
using Netling.Core.Collections;

namespace Netling
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var source = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, o) => source.Cancel();
            var re = new Stream<Uri>(new Uri[]
            {
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/01CC19E60B7FA3C4BB53A421F12B1C7D/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/04A738DC36A644B0921A60D8FF011A40/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/05F55E6DA41A4F2D85D5E93432E058F7/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/18868CBE9CF24A56BF5328087555CCE1/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/1A36739A549F4FB08CA17ECDF1DD25B4/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/1E0EC50B054842E389C0E01F296FE991/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/203483CEBB724CBC8F7764092670D3AA/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2097EDCA14204418AEBA1297FC6A47DC/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/23BB035D7F8D425DA38ABB04437320C8/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/247A3AFE975CE96E257EE29CF095A6C4/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2B211DF87C714C519F1007786CFA9A10/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2B4F8D84B3CC4A09AD745F1DACD285D7/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2C04398F93E3480DB71F834BD3392C03/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2F14DAB489724FA692E60D6514173CDF/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/30E11BA3D6ADFE98B93B0D3C070EB9DE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/327C9852A2A549C891DBEA2A671F1158/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/359730AA72EB4C42B120D7DE626F352F/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/3892098854C541FC92B53C9372CDA697/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/399809D8B5DCBAA0475B7BDF463CC923/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/3D14271AE838D01E082CAD44802F0EC5/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/4B2EFD51E1924F6197F0EE08AD9DDEE5/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/C65B5AB66A7C11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/5F4354389587446990F6B52C01F6D226/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/0CC47E75714D43EC9FFA2FACC0A7A40F/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/0326673197383187F7BCB23BBB66370F/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/1881A2A2CFC040F9A6AA355D6C94D4B0/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/74A4F24A82B434C0E841FBC343C73DE0/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/CA91A1CD924003EB11A3AF77EC3C6743/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/2E8095526A7D11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/4C4D078C6A7D11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/70BFA40E6A7C11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/4B63D26E6A7E11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/5575BE90BF7B11D6A5C70004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/59E3A998A5FF11D99450E9036FBCF482/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/F5148CE7495D4B2F8FC64BCE20950F69/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/0B12C91E75F911D5873E0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/1FEC3F347AB111D5B0F90004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/F8036B7951DF446CB0A8C3EC14E9A654/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/7A558F00124741D8B29F1E32A7BCB044/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/4D74F3DA6A7E11D5A4AE0004AC494FFE/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/00C920ACF0284180BEE8D5BCE1CC4FAD/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/81C58308B9C1425DB353F442BFD5B05C/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/8A8BD094C5380B929F34D371D7E4268C/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/91D3B020E72747B1AA592C2840863BC2/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/967976D13FC243D19AD97354BEE0D14F/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/A22E3E8D1B4F466C8BD116058127DC26/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/B6045ABDDD424F5CAEA16C3C4C9F2C67/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/B63235CF5A094379B98B8DB1822E9B3E/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/BDF3D5116611484EBA094E1EC6617CD9/full.json"),
                new Uri("https://it-wsdev1.s.uw.edu/identity/v2/person/C0EC5B5F3DCAFCB76EF5A5429093A17D/full.json"),
            });
            var worker = new Worker(new NothingWorkJob(), re);

            var opts = new DurationOptions
            {
                Threads = 3,
                Concurrency = 5,
                Duration = TimeSpan.FromSeconds(10)
            };

            //var opts = new CountOptions
            //{
            //    Threads = 3,
            //    Concurrency = 5,
            //    Count = 100
            //};

            var result = await worker.Run("test", opts, source.Token);

        }
    }

    class NothingWorkJob : IWorkerJob
    {
        readonly WorkerThreadResult _result;

        public NothingWorkJob()
        {
            _result = new WorkerThreadResult();
        }

        private NothingWorkJob(WorkerThreadResult result)
        {
            _result = result;
        }

        public async Task DoWork(Uri uri)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5)); // simulate network call
            Console.WriteLine(uri.ToString());
            _result.Add(0, 0, 0, 0, false);
        }

        public WorkerThreadResult GetResults()
        {
            return _result;
        }

        public ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult)
        {
            return new ValueTask<IWorkerJob>(new NothingWorkJob(workerThreadResult));
        }
    }
}
