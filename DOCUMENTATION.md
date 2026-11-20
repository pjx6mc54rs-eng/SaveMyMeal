# 🍽️ SaveMyMeal — Documentation Backend API

Le fichier complet de documentation est consultable à l'emplacement :
👉 [Backend-SaveMyMeal/README.md](file:///Users/mac1/RiderProjects/SaveMyMeal/Backend-SaveMyMeal/README.md)

---

## 🚀 Résumé Express des Commandes de Test pour Ajouter des Plats

Assurez-vous que l'application backend est lancée (`dotnet run` dans `Backend-SaveMyMeal`).

### 1. Créer un commerçant et son restaurant
```bash
# Inscription
curl -X POST http://localhost:5106/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"boulangerie_paris","email":"contact@boulangerie.fr","password":"Password123!","role":"Restaurant"}'

# Création restaurant (Id = 1)
curl -X POST http://localhost:5106/api/restaurants \
  -H "Content-Type: application/json" \
  -d '{"name":"Boulangerie Tradition","address":"15 rue des Martyrs, Paris","phoneNumber":"0140203040","ownerId":1}'
```

### 2. Ajouter des Plats / Paniers Anti-Gaspi (`POST /api/meals`)

#### Plat 1 : Panier Viennoiseries (Expire dans 1 heure -> Remise Flash Sale -75%)
```bash
curl -X POST http://localhost:5106/api/meals \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Panier Surprise Viennoiseries",
    "description": "Croissants, pains au chocolat et brioches du matin.",
    "originalPrice": 16.00,
    "discountedPrice": 4.99,
    "quantity": 5,
    "expiryDate": "'$(date -u -v+1H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+1 hour" +"%Y-%m-%dT%H:%M:%SZ")'",
    "restaurantId": 1,
    "categoryId": 1
  }'
```

#### Plat 2 : Plat Chaud Traiteur (Expire dans 3 heures -> Remise Critique -60%)
```bash
curl -X POST http://localhost:5106/api/meals \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Gratin Dauphinois & Poulet Rôti",
    "description": "Portion copieuse cuisinée maison.",
    "originalPrice": 13.50,
    "discountedPrice": 5.50,
    "quantity": 3,
    "expiryDate": "'$(date -u -v+3H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+3 hours" +"%Y-%m-%dT%H:%M:%SZ")'",
    "restaurantId": 1,
    "categoryId": 1
  }'
```

### 3. Voir les Plats et les Offres Intelligentes
```bash
# Tous les plats disponibles
curl -X GET "http://localhost:5106/api/meals?onlyAvailable=true"

# Offres calculées par l'algorithme Smart Discount (avec niveau d'urgence)
curl -X GET "http://localhost:5106/api/offers"
```
