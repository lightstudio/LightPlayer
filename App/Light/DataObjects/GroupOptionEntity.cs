namespace Light.DataObjects
{
    public class GroupOptionEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public GroupOptionEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static GroupOptionEntity CreateAtoZ() => new GroupOptionEntity(0, App.ResourceLoader.GetString("GAtoZ"));

        public static GroupOptionEntity CreateZtoA() => new GroupOptionEntity(1, App.ResourceLoader.GetString("GZtoA"));

        public static GroupOptionEntity CreateNoGroup() => new GroupOptionEntity(2, App.ResourceLoader.GetString("GNoGroup"));

        public static GroupOptionEntity CreateGenre() => new GroupOptionEntity(3, App.ResourceLoader.GetString("GGenre"));

        public static GroupOptionEntity CreateReleaseDate() => new GroupOptionEntity(4, App.ResourceLoader.GetString("GReleaseDate"));
    }
}
