from sqlalchemy.orm import Session
from schemas import Character, Conversation, Resumen, Folder
import os
# ===========================
# GESTIÓN DE PERSONAJES
# ===========================

def get_character_by_id(db: Session, character_id: int):
    return db.query(Character).filter(Character.id == character_id).first()

def get_characters(db: Session):
    return db.query(Character).all()

def add_character(db: Session, name: str, age: str, description: str, epoca: str):
    new_char = Character(name=name, age=age, description=description, epoca=epoca)
    db.add(new_char)
    db.commit()
    db.refresh(new_char)
    return new_char

def delete_character(db: Session, char_id: int):
    db_char = db.query(Character).filter(Character.id == char_id).first()
    if db_char:
        db.delete(db_char) # El cascade en schemas.py borra conversaciones y resúmenes automáticamente
        db.commit()
        return True
    return False

# ===========================
# GESTIÓN DE CONVERSACIONES
# ===========================

def add_conversation(db: Session, character_id: int, user_m: str, npc_m: str):
    new_convo = Conversation(character_id=character_id, message_user=user_m, message_npc=npc_m)
    db.add(new_convo)
    db.commit()
    db.refresh(new_convo)
    return new_convo

def get_conversations(db: Session, character_id: int):
    return db.query(Conversation)\
             .filter(Conversation.character_id == character_id)\
             .order_by(Conversation.created_at.asc())\
             .all()

# ===========================
# GESTIÓN DE RESUMEN (MEMORIA LARGO PLAZO)
# ===========================

def update_or_create_resumen(db: Session, character_id: int, text: str):
    res = db.query(Resumen).filter(Resumen.character_id == character_id).first()
    if res:
        res.resumen_text = text
    else:
        res = Resumen(character_id=character_id, resumen_text=text)
        db.add(res)
    db.commit()
    db.refresh(res)
    return res

# ===========================
# SISTEMA DE ARCHIVOS Y RESET
# ===========================

def update_or_create_root_folder(db: Session, name: str):
    internal_route = os.path.join("uploads", name)

    db_folder = db.query(Folder).filter(Folder.id == 1).first()

    if db_folder:
        db_folder.name = name
        db_folder.route = internal_route
    else:
        db_folder = Folder(id=1, name=name, route=internal_route)
        db.add(db_folder)

    db.commit()
    db.refresh(db_folder)
    return db_folder

def reset_all_tables(db: Session):
    try:
        db.query(Conversation).delete()
        db.query(Resumen).delete()
        db.query(Character).delete()
        db.query(Folder).delete()
        db.commit()
        return True
    except Exception as e:
        db.rollback()
        raise e