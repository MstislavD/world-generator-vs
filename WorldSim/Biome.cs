namespace WorldSim
{
    public struct Biome
    {
        public static bool operator ==(Biome b1, Biome b2) => b1.Equals(b2);
        public static bool operator !=(Biome b1, Biome b2) => !b1.Equals(b2);
        public string Name { get; }
        public Belt Belt { get; }
        public Humidity Humidity { get; }
        public Biome(string name, Belt belt, Humidity humidity)
        {
            Name = name;
            Belt = belt;
            Humidity = humidity;
        }
        public override string ToString() => Name;
    }
}
