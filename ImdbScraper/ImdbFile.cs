using System.Collections.Generic;

namespace ImdbScraper
{
    public class ImdbFile
    {
        public string Titulo;

        public string Ano;

        public List<ImdbReview> Reviews;

        public ImdbFile(string pTitulo, string pAno, List<ImdbReview> pReviews)
        {
            Titulo = pTitulo;
            Ano = pAno;
            Reviews = pReviews;
        }
    }
}