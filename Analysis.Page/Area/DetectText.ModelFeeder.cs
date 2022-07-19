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


            public ModelFeeder(int size)
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
                model.LoadedModel.Dispose();

                models.Enqueue(new Model()
                {
                    Id = model.Id,
                    UsedCount = ++model.UsedCount,
                    LoadedModel = DetectText.NewModel()
                });
            }

        }
    }
}
