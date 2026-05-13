from sqlalchemy.orm import Session
from schemas import Character, Conversation, Resumen, Folder, StoredFile
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
def save_file(
    db: Session,
    folder_name: str,
    filename: str,
    content_type: str,
    data: bytes
):
    new_file = StoredFile(
        folder_name=folder_name,
        filename=filename,
        content_type=content_type,
        data=data
    )
    db.add(new_file)
    db.commit()
    db.refresh(new_file)
    return new_file

def get_file_by_id(db: Session, file_id: int):
    return db.query(StoredFile)\
             .filter(StoredFile.id == file_id)\
             .first()

def get_files_by_folder(db: Session, folder_name: str):
    return db.query(StoredFile)\
             .filter(StoredFile.folder_name == folder_name)\
             .order_by(StoredFile.created_at.asc())\
             .all()

def delete_file(db: Session, file_id: int):
    file = db.query(StoredFile)\
             .filter(StoredFile.id == file_id)\
             .first()
    if not file:
        return False
    db.delete(file)
    db.commit()
    return True

def get_unique_folders(db: Session):
    folders = db.query(StoredFile.folder_name).distinct().all()
    return [f[0] for f in folders]

def reset_all_tables(db: Session):
    try:
        db.query(Conversation).delete()
        db.query(Resumen).delete()
        db.query(Character).delete()
        db.query(StoredFile).delete()
        db.commit()
        return True
    except Exception as e:
        db.rollback()
        raise e