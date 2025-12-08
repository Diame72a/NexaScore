import sys
import json
import re
import pdfplumber


sys.stdout.reconfigure(encoding='utf-8')

from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity


FRENCH_STOP_WORDS = [
    "au", "aux", "avec", "ce", "ces", "dans", "de", "des", "du", "elle", "en", "et", "eux", "il", 
    "je", "la", "le", "leur", "lui", "ma", "mais", "me", "meme", "mes", "moi", "mon", "ne", "nos", 
    "notre", "nous", "on", "ou", "par", "pas", "pour", "qu", "que", "qui", "sa", "se", "ses", 
    "son", "sur", "ta", "te", "tes", "toi", "ton", "tu", "un", "une", "vos", "votre", "vous",
    "c", "d", "j", "l", "m", "n", "s", "t", "y", "été", "étée", "étées", "étés", "étant", "suis", "es", "est",
    "les", "a", "à", "y", "or", "ni", "car", "donc", "lorsque", "puisque", "quand", "comme", "si"
]

def clean_text(text):
    """
    Nettoie le texte en gardant les termes techniques (C#, .NET, C++, etc.)
    """
    if not text: return ""
    text = text.lower()
    

    text = text.replace('\n', ' ').replace('\r', '')
    

    text = re.sub(r'[^a-z0-9àâçéèêëîïôûùüÿñæœ\+\#\.]', ' ', text)
    
 
    text = re.sub(r'(?<!\w)\.(?!\w)', ' ', text)
    

    text = re.sub(r'\s+', ' ', text).strip()
    
    return text

def extract_text_from_pdf(pdf_path):
    """Extrait le texte du PDF via pdfplumber"""
    full_text = ""
    try:
        with pdfplumber.open(pdf_path) as pdf:
            for page in pdf.pages:
                extracted = page.extract_text()
                if extracted:
                    full_text += extracted + " "
    except Exception:
        return ""
    return full_text

def get_common_words(text1, text2):
    """
    Récupère la liste des mots identiques entre le CV et l'Offre.
    Gère les exceptions pour les mots courts techniques (R, C, C#).
    """
    set1 = set(text1.split())
    set2 = set(text2.split())
    

    common = set1.intersection(set2)
    
    filtered = []
    for w in common:

        is_technical = '+' in w or '#' in w or w in ['r', 'c', 'go', 'js', 'ai']
        

        if w not in FRENCH_STOP_WORDS:

            if len(w) > 2 or is_technical:
                filtered.append(w)
                
    return list(filtered)

if __name__ == "__main__":

    response = { "success": False, "score": 0, "matches": [], "message": "" }

    try:

        if len(sys.argv) < 3: 
            raise ValueError("Arguments manquants (Attendu: script.py pdf_path description)")

        pdf_path = sys.argv[1]
        job_desc = sys.argv[2] 
        

        cv_text = extract_text_from_pdf(pdf_path)
        
        if not cv_text or not cv_text.strip():
            response["message"] = "Le PDF semble vide ou est une image scannée."
            response["score"] = 0
        else:

            cv_clean = clean_text(cv_text)
            job_clean = clean_text(job_desc)
            
            if not cv_clean or not job_clean:
                response["score"] = 0
                response["message"] = "Texte vide après nettoyage."
            else:

                documents = [cv_clean, job_clean]
                try:

                    tfidf = TfidfVectorizer(
                        stop_words=FRENCH_STOP_WORDS, 
                        ngram_range=(1, 3), 
                        token_pattern=r"(?u)\b[\w\+\#\.]+\b"
                    )
                    tfidf_matrix = tfidf.fit_transform(documents)
                    sim_matrix = cosine_similarity(tfidf_matrix[0:1], tfidf_matrix[1:2])
                    raw_score = sim_matrix[0][0] * 100
                except:
                    raw_score = 0


                matches = get_common_words(cv_clean, job_clean)
                

                points_par_mot = 12.0 
                bonus_score = len(matches) * points_par_mot
                

                final_score = raw_score + bonus_score
                

                if final_score > 100: final_score = 100
                if final_score < 0: final_score = 0
                

                response["score"] = round(final_score, 0)
                response["success"] = True
                response["matches"] = matches
                response["message"] = "Analyse terminée avec succès."

    except Exception as e:
        response["success"] = False
        response["message"] = f"Erreur interne Python : {str(e)}"


    print(json.dumps(response, ensure_ascii=False))