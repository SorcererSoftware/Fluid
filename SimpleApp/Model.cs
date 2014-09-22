namespace SimpleApp {
   class Model {
      public string Original { get; set; }

      internal string SecretLogic() {
         return Original.ToUpper();
      }
   }
}
