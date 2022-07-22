using OpenCvSharp.Dnn;

namespace HlidacStatu.Analysis.Page.Area
{
    public partial class DetectText
    {
        public class ModelFeeder
        {
            public class Model
            {
                public int Id { get; set; }
                public int UsedCount { get; internal set; } = 0;
                public Net LoadedModel { get; set; } = null;
            }

            System.Collections.Concurrent.ConcurrentQueue<Model> models =
                new System.Collections.Concurrent.ConcurrentQueue<Model>();
            private readonly bool reUseModels;

            public ModelFeeder(int size, bool reUseModels = true)
            {
                Console.Write("Loading models");
                for (int i = 0; i < size; i++)
                {
                    models.Enqueue(new Model()
                    {
                        Id = i,
                        UsedCount = 0,
                        LoadedModel = DetectText.NewModel()
                    });
                    Console.Write(".");
                }
                Console.WriteLine(" done");
                this.reUseModels = reUseModels;
            }

            public int FreeModels()
            {
                return models.Count;
            }
            public Model GetFreeModel()
            {
                int tries = 0;
                Model model = null;
                do
                {
                    if (models.TryDequeue(out model))
                    {
                        return model;
                    }
                    else
                    {
                        tries++;
                        System.Threading.Thread.Sleep(1000*tries);
                    }
                } while (tries<10);

                throw new ApplicationException("No free model");
            }

            public void ReturnModelBack(Model model)
            {
                if (this.reUseModels)
                {
                    model.UsedCount++;
                    models.Enqueue(model);
                }
                else
                {
                    model.LoadedModel.Dispose();
                    models.Enqueue(new Model()
                    {
                        Id = model.Id,
                        UsedCount = ++model.UsedCount,
                        LoadedModel = DetectText.NewModel()
                    });
                    model = null;
                }
            }

        }
    }
}
