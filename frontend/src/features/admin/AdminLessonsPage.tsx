import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  createAdminLesson,
  deleteAdminLesson,
  getAdminApiError,
  getAdminFolders,
  getAdminLesson,
  getAdminLessons,
  updateAdminLesson,
  type AdminFolder,
  type AdminLesson,
} from './adminApi';
import './adminLessons.css';

type JsonValue = string | number | boolean | null | JsonValue[] | JsonObject;
type JsonObject = { [key: string]: JsonValue };
type Question = JsonObject;

function isJsonObject(value: JsonValue | undefined): value is JsonObject {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function parseJson(value: string): JsonValue | undefined {
  try {
    return JSON.parse(value) as JsonValue;
  } catch {
    return undefined;
  }
}

function getQuestions(value: JsonValue | undefined): Question[] | null {
  if (!isJsonObject(value) || !Array.isArray(value.questions) ||
      !value.questions.every(isJsonObject)) {
    return null;
  }

  return value.questions;
}

function getText(value: JsonValue | undefined): string {
  return typeof value === 'string' ? value : value == null ? '' : String(value);
}

function hasInvalidFileNameCharacters(name: string): boolean {
  return /[\\/:*?"<>|]/.test(name) ||
    name.split('').some((character) => {
      const code = character.charCodeAt(0);
      return code < 32 || code === 127;
    });
}

function defaultContent(folder: AdminFolder | undefined): string {
  if (folder?.path.split('/')[0].toLowerCase() === 'vocab') {
    return JSON.stringify([{
      id: 1,
      word: '',
      meaning: '',
      phonetic: '',
      exampleSentence: '',
    }], null, 2);
  }

  return JSON.stringify({
    name: '',
    type: '',
    questions: [],
    file_dinh_kem: '',
  }, null, 2);
}

export default function AdminLessonsPage() {
  const [folders, setFolders] = useState<AdminFolder[]>([]);
  const [lessons, setLessons] = useState<AdminLesson[]>([]);
  const [selectedFolderId, setSelectedFolderId] = useState('');
  const [isLoadingFolders, setIsLoadingFolders] = useState(true);
  const [isLoadingLessons, setIsLoadingLessons] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [openingLessonId, setOpeningLessonId] = useState<number | null>(null);
  const [deletingLessonId, setDeletingLessonId] = useState<number | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editingLessonId, setEditingLessonId] = useState<number | null>(null);
  const [editorFolderId, setEditorFolderId] = useState('');
  const [title, setTitle] = useState('');
  const [lessonType, setLessonType] = useState<'lesson' | 'test'>('lesson');
  const [jsonContent, setJsonContent] = useState('');
  const [pageError, setPageError] = useState('');
  const [editorError, setEditorError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedFolder = useMemo(
    () => folders.find((folder) => folder.id === selectedFolderId),
    [folders, selectedFolderId],
  );

  const refreshLessons = useCallback(async () => {
    setIsLoadingLessons(true);
    setPageError('');
    try {
      setLessons(await getAdminLessons(selectedFolderId || undefined));
    } catch (error: unknown) {
      setPageError(getAdminApiError(error));
    } finally {
      setIsLoadingLessons(false);
    }
  }, [selectedFolderId]);

  const refreshFolders = useCallback(async () => {
    setIsLoadingFolders(true);
    try {
      setFolders(await getAdminFolders());
    } catch (error: unknown) {
      setPageError(getAdminApiError(error));
    } finally {
      setIsLoadingFolders(false);
    }
  }, []);

  useEffect(() => {
    let isActive = true;
    getAdminFolders()
      .then((result) => {
        if (isActive) {
          setFolders(result);
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setPageError(getAdminApiError(error));
        }
      })
      .finally(() => {
        if (isActive) {
          setIsLoadingFolders(false);
        }
      });

    return () => {
      isActive = false;
    };
  }, []);

  useEffect(() => {
    let isActive = true;
    getAdminLessons(selectedFolderId || undefined)
      .then((result) => {
        if (isActive) {
          setLessons(result);
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setPageError(getAdminApiError(error));
        }
      })
      .finally(() => {
        if (isActive) {
          setIsLoadingLessons(false);
        }
      });

    return () => {
      isActive = false;
    };
  }, [selectedFolderId]);

  const openCreate = () => {
    const folder = selectedFolder ?? folders[0];
    setEditingLessonId(null);
    setEditorFolderId(folder?.id ?? '');
    setTitle('');
    setLessonType('lesson');
    setJsonContent(defaultContent(folder));
    setEditorError('');
    setEditorOpen(true);
  };

  const openEdit = async (lesson: AdminLesson) => {
    setOpeningLessonId(lesson.id);
    setPageError('');
    setEditorError('');
    try {
      const content = await getAdminLesson(lesson.id);
      setEditingLessonId(content.id);
      setEditorFolderId(content.folderId);
      setTitle(content.title);
      setLessonType(content.lessonType);
      setJsonContent(content.content);
      setEditorOpen(true);
    } catch (error: unknown) {
      setPageError(getAdminApiError(error));
    } finally {
      setOpeningLessonId(null);
    }
  };

  const closeEditor = () => {
    if (!isSaving) {
      setEditorOpen(false);
      setEditorError('');
    }
  };

  const saveLesson = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setEditorError('');

    const normalizedTitle = title.trim();
    if (!normalizedTitle || hasInvalidFileNameCharacters(normalizedTitle)) {
      setEditorError('Tiêu đề không hợp lệ. Không nhập đường dẫn hoặc ký tự đặc biệt.');
      return;
    }
    if (!editingLessonId && !editorFolderId) {
      setEditorError('Vui lòng chọn thư mục lưu bài học.');
      return;
    }

    const parsedContent = parseJson(jsonContent);
    if (parsedContent === undefined ||
        (typeof parsedContent !== 'object' || parsedContent === null)) {
      setEditorError('Nội dung phải là JSON hợp lệ dạng object hoặc array.');
      return;
    }

    setIsSaving(true);
    try {
      if (editingLessonId) {
        await updateAdminLesson(editingLessonId, {
          title: normalizedTitle,
          lessonType,
          content: jsonContent,
        });
        setNotice('Đã cập nhật bài học trên SQL Server và Google Drive.');
      } else {
        await createAdminLesson({
          folderId: editorFolderId,
          title: normalizedTitle,
          lessonType,
          content: jsonContent,
        });
        setNotice('Đã tạo bài học trên SQL Server và Google Drive.');
      }
      setEditorOpen(false);
      await Promise.all([refreshLessons(), refreshFolders()]);
    } catch (error: unknown) {
      setEditorError(getAdminApiError(error));
    } finally {
      setIsSaving(false);
    }
  };

  const removeLesson = async (lesson: AdminLesson) => {
    if (!window.confirm(`Bạn có chắc muốn xóa bài học "${lesson.title}" khỏi SQL Server và Google Drive?`)) {
      return;
    }

    setDeletingLessonId(lesson.id);
    setPageError('');
    setNotice('');
    try {
      await deleteAdminLesson(lesson.id);
      setNotice(`Đã xóa ${lesson.title}.`);
      await refreshLessons();
    } catch (error: unknown) {
      setPageError(getAdminApiError(error));
    } finally {
      setDeletingLessonId(null);
    }
  };

  const setQuestionField = (index: number, field: string, value: string) => {
    const parsed = parseJson(jsonContent);
    const questions = getQuestions(parsed);
    if (!isJsonObject(parsed) || !questions) {
      return;
    }

    const updatedQuestions = questions.map((question, questionIndex) =>
      questionIndex === index ? { ...question, [field]: value } : question);
    setJsonContent(JSON.stringify({ ...parsed, questions: updatedQuestions }, null, 2));
  };

  const addQuestion = () => {
    const parsed = parseJson(jsonContent);
    if (!isJsonObject(parsed)) {
      return;
    }

    const questions = getQuestions(parsed) ?? [];
    const updatedQuestions = [
      ...questions,
      { question: '', '1': '', '2': '', '3': '', '4': '', 'correct-ans': '1' },
    ];
    setJsonContent(JSON.stringify({ ...parsed, questions: updatedQuestions }, null, 2));
  };

  const removeQuestion = (index: number) => {
    const parsed = parseJson(jsonContent);
    const questions = getQuestions(parsed);
    if (!isJsonObject(parsed) || !questions) {
      return;
    }

    setJsonContent(JSON.stringify({
      ...parsed,
      questions: questions.filter((_, questionIndex) => questionIndex !== index),
    }, null, 2));
  };

  const questions = getQuestions(parseJson(jsonContent));

  return (
    <section className="admin-lessons">
      <header className="admin-lessons__header">
        <div>
          <p className="admin-lessons__eyebrow">KHU VỰC QUẢN TRỊ</p>
          <h1>Quản lý nội dung học tập</h1>
          <p>Thông tin bài học lưu trên SQL Server, nội dung JSON lưu trên Google Drive.</p>
        </div>
        <div className="admin-lessons__header-actions">
          <button
            className="admin-lessons__button admin-lessons__button--secondary"
            type="button"
            onClick={() => { void Promise.all([refreshFolders(), refreshLessons()]); }}
            disabled={isLoadingFolders || isLoadingLessons}
          >
            Tải lại
          </button>
          <button
            className="admin-lessons__button"
            type="button"
            onClick={openCreate}
            disabled={isLoadingFolders || folders.length === 0}
          >
            + Thêm bài học
          </button>
        </div>
      </header>

      <div className="admin-lessons__toolbar">
        <label htmlFor="admin-folder-filter">Thư mục nội dung</label>
        <select
          id="admin-folder-filter"
          value={selectedFolderId}
          onChange={(event) => {
            setIsLoadingLessons(true);
            setPageError('');
            setSelectedFolderId(event.target.value);
          }}
          disabled={isLoadingFolders}
        >
          <option value="">Tất cả thư mục</option>
          {folders.map((folder) => (
            <option key={folder.id} value={folder.id}>{folder.path}</option>
          ))}
        </select>
        <span className="admin-lessons__count">
          {isLoadingLessons ? 'Đang tải...' : `${lessons.length} bài học`}
        </span>
      </div>

      {pageError && <p className="admin-lessons__message admin-lessons__message--error" role="alert">{pageError}</p>}
      {notice && <p className="admin-lessons__message admin-lessons__message--success" role="status">{notice}</p>}
      {isLoadingFolders && <p className="admin-lessons__state">Đang tải thư mục Google Drive...</p>}
      {!isLoadingFolders && folders.length === 0 && !pageError && (
        <p className="admin-lessons__state">Không tìm thấy thư mục nội dung trong Google Drive.</p>
      )}
      {isLoadingLessons && <p className="admin-lessons__state">Đang tải nội dung...</p>}
      {!isLoadingLessons && lessons.length === 0 && !pageError && folders.length > 0 && (
        <p className="admin-lessons__state">
          {selectedFolder
            ? `Chưa có bài học trong ${selectedFolder.path} hoặc các thư mục con.`
            : 'Chưa có bài học trong các thư mục được quản lý.'}
        </p>
      )}

      {!isLoadingLessons && lessons.length > 0 && (
        <div className="admin-lessons__table-wrap">
          <table className="admin-lessons__table">
            <thead>
              <tr>
                <th>Tiêu đề bài học</th>
                <th>Thư mục</th>
                <th>Cập nhật</th>
                <th><span className="visually-hidden">Thao tác</span></th>
              </tr>
            </thead>
            <tbody>
              {lessons.map((lesson) => (
                <tr key={lesson.id}>
                  <td data-label="Tên file">
                    <strong>{lesson.title}</strong>
                    <span className="admin-lessons__file-kind">{lesson.lessonType}</span>
                  </td>
                  <td data-label="Thư mục">{lesson.folderPath}</td>
                  <td data-label="Cập nhật">
                    {lesson.modifiedTime
                      ? new Date(lesson.modifiedTime).toLocaleString()
                      : '—'}
                  </td>
                  <td className="admin-lessons__row-actions" data-label="Thao tác">
                    <button
                      className="admin-lessons__text-button"
                      type="button"
                      onClick={() => { void openEdit(lesson); }}
                      disabled={openingLessonId === lesson.id || deletingLessonId === lesson.id}
                    >
                      {openingLessonId === lesson.id ? 'Đang mở...' : 'Sửa'}
                    </button>
                    <button
                      className="admin-lessons__text-button admin-lessons__text-button--danger"
                      type="button"
                      onClick={() => { void removeLesson(lesson); }}
                      disabled={deletingLessonId === lesson.id || openingLessonId === lesson.id}
                    >
                      {deletingLessonId === lesson.id ? 'Đang xóa...' : 'Xóa'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editorOpen && (
        <div className="admin-lessons__backdrop" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget) closeEditor();
        }}>
          <section
            className="admin-lessons__dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="admin-editor-title"
          >
            <header className="admin-lessons__dialog-header">
              <div>
                <p className="admin-lessons__eyebrow">{editingLessonId ? 'CHỈNH SỬA BÀI HỌC' : 'BÀI HỌC MỚI'}</p>
                <h2 id="admin-editor-title">{editingLessonId ? 'Sửa nội dung' : 'Thêm bài học'}</h2>
              </div>
              <button className="admin-lessons__close" type="button" onClick={closeEditor} aria-label="Đóng">
                ×
              </button>
            </header>

            <form className="admin-lessons__form" onSubmit={(event) => { void saveLesson(event); }}>
              {editorError && (
                <p className="admin-lessons__message admin-lessons__message--error" role="alert">{editorError}</p>
              )}
              {!editingLessonId && (
                <label className="admin-lessons__field">
                  <span>Thư mục lưu</span>
                  <select value={editorFolderId} onChange={(event) => setEditorFolderId(event.target.value)} required>
                    {folders.map((folder) => (
                      <option key={folder.id} value={folder.id}>{folder.path}</option>
                    ))}
                  </select>
                </label>
              )}
              <label className="admin-lessons__field">
                <span>Tiêu đề</span>
                <input
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Ví dụ: Bài 1 - Vocabulary"
                  maxLength={180}
                  required
                />
                <small>Tiêu đề được lưu trong SQL; tên file JSON trên Drive được tạo từ tiêu đề.</small>
              </label>
              <label className="admin-lessons__field">
                <span>Loại bài học</span>
                <select
                  value={lessonType}
                  onChange={(event) => setLessonType(event.target.value as 'lesson' | 'test')}
                >
                  <option value="lesson">Bài học</option>
                  <option value="test">Bài kiểm tra</option>
                </select>
              </label>
              {questions && (
                <div className="admin-lessons__questions">
                  <div className="admin-lessons__section-heading">
                    <div>
                      <strong>Câu hỏi trắc nghiệm</strong>
                      <span>{questions.length} câu</span>
                    </div>
                    <button className="admin-lessons__text-button" type="button" onClick={addQuestion}>
                      + Thêm câu hỏi
                    </button>
                  </div>
                  {questions.map((question, index) => (
                    <fieldset className="admin-lessons__question" key={`question-${index}`}>
                      <legend>Câu {index + 1}</legend>
                      <label className="admin-lessons__field">
                        <span>Nội dung câu hỏi</span>
                        <textarea
                          value={getText(question.question)}
                          onChange={(event) => setQuestionField(index, 'question', event.target.value)}
                          rows={2}
                        />
                      </label>
                      <div className="admin-lessons__choices">
                        {['1', '2', '3', '4'].map((choice) => (
                          <label className="admin-lessons__field" key={choice}>
                            <span>Lựa chọn {choice}</span>
                            <input
                              value={getText(question[choice])}
                              onChange={(event) => setQuestionField(index, choice, event.target.value)}
                            />
                          </label>
                        ))}
                      </div>
                      <div className="admin-lessons__question-footer">
                        <label className="admin-lessons__field">
                          <span>Đáp án đúng</span>
                          <select
                            value={getText(question['correct-ans']) || '1'}
                            onChange={(event) => setQuestionField(index, 'correct-ans', event.target.value)}
                          >
                            {['1', '2', '3', '4'].map((choice) => (
                              <option value={choice} key={choice}>Lựa chọn {choice}</option>
                            ))}
                          </select>
                        </label>
                        <button
                          className="admin-lessons__text-button admin-lessons__text-button--danger"
                          type="button"
                          onClick={() => removeQuestion(index)}
                        >
                          Xóa câu hỏi
                        </button>
                      </div>
                    </fieldset>
                  ))}
                </div>
              )}
              <label className="admin-lessons__field">
                <span>Nội dung JSON</span>
                <textarea
                  className="admin-lessons__json"
                  value={jsonContent}
                  onChange={(event) => setJsonContent(event.target.value)}
                  rows={15}
                  spellCheck={false}
                  required
                />
                <small>Giữ nguyên các trường chưa chỉnh sửa. Có thể sửa trực tiếp JSON nếu cấu trúc khác.</small>
              </label>
              <footer className="admin-lessons__form-actions">
                <button
                  className="admin-lessons__button admin-lessons__button--secondary"
                  type="button"
                  onClick={closeEditor}
                  disabled={isSaving}
                >
                  Hủy
                </button>
                <button className="admin-lessons__button" type="submit" disabled={isSaving}>
                  {isSaving ? 'Đang lưu...' : editingLessonId ? 'Lưu thay đổi' : 'Tạo bài học'}
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}
