(()=>{
const $=(s,e=document)=>e.querySelector(s),$$=(s,e=document)=>[...e.querySelectorAll(s)];
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const fmt=d=>d?new Date(d).toLocaleString('id-ID',{dateStyle:'medium',timeStyle:'short'}):'-';
const size=n=>n<1024?n+' B':n<1048576?(n/1024).toFixed(1)+' KB':n<1073741824?(n/1048576).toFixed(1)+' MB':(n/1073741824).toFixed(2)+' GB';
const short=g=>g?String(g).slice(0,8):'-';
const rh=n=>Array.from({length:n},()=>'0123456789abcdef'[Math.random()*16|0]).join('');
const en=(v,n)=>typeof v==='number'?n[v]:v;
const reduce=matchMedia('(prefers-reduced-motion:reduce)').matches;
const root=$('#root');
let S={token:sessionStorage.getItem('ex_t'),user:null},rt=0,evPage=1,evCid='',auLast=0;

function parse(t){try{const p=JSON.parse(atob(t.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));
 const R='http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
 return{id:p.sub||p.nameid,email:p.email||'',roles:[].concat(p[R]||p.role||[]),exp:p.exp}}catch{return null}}
async function api(path,o={}){
 let r;try{r=await fetch('/api'+path,{method:o.method||'GET',headers:{...(S.token?{Authorization:'Bearer '+S.token}:{}),...(o.json?{'Content-Type':'application/json'}:{})},body:o.json?JSON.stringify(o.json):undefined})}
 catch{throw Error('Server tidak dapat dihubungi.')}
 if(r.status===401&&S.token&&!o.anon){logout();throw Error('Sesi berakhir. Silakan masuk lagi.')}
 if(r.status===403)throw Error('Anda tidak memiliki izin untuk aksi ini.');
 if(!r.ok){let m=await r.text();try{const j=JSON.parse(m);m=j.message||j.title||(Array.isArray(j)?j.map(e=>e.description).join(' '):m)}catch{}throw Error(m||'Permintaan gagal ('+r.status+').')}
 if(o.blob)return r.blob();const t=await r.text();return t?JSON.parse(t):null}

function toast(m,t=''){const e=document.createElement('div');e.className='toast align-items-center border-0 text-bg-'+(t==='bad'?'danger':t==='ok'?'success':'dark');e.setAttribute('role','alert');
 e.innerHTML=`<div class="d-flex"><div class="toast-body"></div><button class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Tutup"></button></div>`;$('.toast-body',e).textContent=m;
 $('#toasts').append(e);e.addEventListener('hidden.bs.toast',()=>e.remove());new bootstrap.Toast(e,{delay:3800}).show()}
function dialog(title,body,cls=''){const dr=cls==='drw',k=dr?'offcanvas':'modal',d=document.createElement('div');d.tabIndex=-1;
 d.className=(dr?'offcanvas offcanvas-end':'modal fade')+' ovr';
 d.innerHTML=dr?`<div class="offcanvas-header"><h3 class="offcanvas-title">${title}</h3><button class="btn-close" data-bs-dismiss="offcanvas" aria-label="Tutup"></button></div><div class="offcanvas-body bd">${body}</div>`
 :`<div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h3 class="modal-title">${title}</h3><button class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button></div><div class="modal-body bd">${body}</div></div></div>`;
 document.body.append(d);const i=dr?new bootstrap.Offcanvas(d):new bootstrap.Modal(d);
 d.close=()=>d.classList.contains('show')?i.hide():d.addEventListener('shown.bs.'+k,()=>i.hide(),{once:true});
 d.addEventListener('hidden.bs.'+k,()=>{i.dispose();d.remove()});i.show();return d}
function formDlg(t,fs,ok,fn){
 const d=dialog(t,`<form>${fs.map(([n,l,ty,rq])=>`<label class="form-label w-100 mb-3">${l}${ty==='area'?`<textarea class="form-control" name="${n}" rows="3"></textarea>`:`<input class="form-control" name="${n}" ${rq?'required':''} ${ty==='uuid'?'pattern="[0-9a-fA-F-]{36}" placeholder="ID pengguna penerima"':''}>`}</label>`).join('')}<p class="err" role="alert"></p><button class="btn btn-warning pri">${ok}</button></form>`);
 const f=$('form',d);$('input,textarea',f).focus();
 f.onsubmit=async e=>{e.preventDefault();const b=$('.btn',f);b.disabled=true;
  try{await fn(Object.fromEntries(new FormData(f)),d)}catch(x){$('.err',f).textContent=x.message;f.classList.remove('shake');void f.offsetWidth;f.classList.add('shake');b.disabled=false}}}
function scramble(el,txt,ms=900){return new Promise(res=>{if(reduce){el.textContent=txt;return res()}const t0=performance.now();
 (function f(){const p=Math.min(1,(performance.now()-t0)/ms),k=Math.floor(p*txt.length);el.textContent=txt.slice(0,k)+rh(txt.length-k);p<1?requestAnimationFrame(f):res()})()})}
function countUp(el){const n=+el.dataset.n;if(reduce||!n){el.textContent=n;return}const t0=performance.now();
 (function f(t){const p=Math.min(1,(t-t0)/900);el.textContent=Math.round(n*(1-Math.pow(1-p,3)));p<1&&requestAnimationFrame(f)})(t0)}
const empty=(t,p)=>`<div class="pn em"><h3>${t}</h3><p>${p}</p></div>`;

/* ---------- masuk dan daftar ---------- */
function authView(){
 let m='in';const hs=['Bukti terdaftar','Kustodi berpindah','Integritas diverifikasi','Peristiwa dicatat','Rantai tak terputus'];
 root.innerHTML=`<div class="tw-grid tw-min-h-screen lg:tw-grid-cols-[1.1fr_1fr] auth"><section class="tw-bg-[#101820] tw-text-[#E7EBE8] tw-px-8 lg:tw-px-16 tw-py-12 tw-flex tw-flex-col tw-justify-between tw-gap-8 hero"><div class="logo" style="padding:0">Evidence<b>X</b></div><div><h1>Setiap bukti punya riwayat yang tidak bisa diubah.</h1><p>Daftarkan barang bukti digital, lacak siapa yang memegangnya, dan buktikan keasliannya dengan SHA-256.</p>
 <ol class="chain">${hs.map((t,i)=>`<li style="--i:${i}"><span class="mk">#${i+1}</span><div>${t}<code data-h>&nbsp;</code></div></li>`).join('')}</ol></div></section><section class="tw-grid tw-place-items-center tw-p-8"><form class="tw-w-full tw-max-w-[380px]" id="af"></form></section></div>`;
 $$('[data-h]').forEach((c,i)=>setTimeout(()=>scramble(c,rh(32),1000),i*450+600));
 const hero=$('.hero');hero.onpointermove=e=>{const r=hero.getBoundingClientRect();hero.style.setProperty('--mx',e.clientX-r.left+'px');hero.style.setProperty('--my',e.clientY-r.top+'px')};
 const hh=$$('[data-h]'),iv=setInterval(()=>{if(!hh[0].isConnected)return clearInterval(iv);if(!reduce)scramble(hh[Math.random()*hh.length|0],rh(32),700)},2600);
 const f=$('#af');
 const draw=()=>{f.innerHTML=`<div class="nav nav-tabs mb-4" role="tablist"><button type="button" role="tab" class="nav-link ${m==='in'?'active':''}" aria-selected="${m==='in'}" data-m="in">Masuk</button><button type="button" role="tab" class="nav-link ${m==='up'?'active':''}" aria-selected="${m==='up'}" data-m="up">Daftar</button></div>
 ${m==='up'?'<label class="form-label w-100 mb-3">Nama tampilan<input class="form-control" name="n" required autocomplete="name"></label>':''}<label class="form-label w-100 mb-3">Email<input class="form-control" name="e" type="email" required autocomplete="email"></label><label class="form-label w-100 mb-3">Kata sandi<input class="form-control" name="p" type="password" required autocomplete="${m==='in'?'current-password':'new-password'}"></label><p class="err" role="alert"></p><button class="btn btn-warning pri" style="width:100%">${m==='in'?'Masuk':'Buat akun'}</button>${m==='up'?'<p class="hint" style="margin-top:.8rem">Akun baru otomatis mendapat peran Investigator.</p>':''}`};
 draw();
 f.onclick=e=>{const t=e.target.closest('[data-m]');if(t){m=t.dataset.m;draw()}};
 f.onsubmit=async ev=>{ev.preventDefault();const d=Object.fromEntries(new FormData(f)),b=$('.btn.pri',f);b.disabled=true;
  try{if(m==='up')await api('/auth/register',{method:'POST',json:{email:d.e,password:d.p,displayName:d.n},anon:1});
   const r=await api('/auth/login',{method:'POST',json:{email:d.e,password:d.p},anon:1});setToken(r.accessToken)}
  catch(x){$('.err',f).textContent=x.message;f.classList.remove('shake');void f.offsetWidth;f.classList.add('shake');b.disabled=false}}}
function setToken(t){S.token=t;sessionStorage.setItem('ex_t',t);S.user=parse(t);boot()}
function logout(){S.token=S.user=null;sessionStorage.removeItem('ex_t');$$('.offcanvas-backdrop').forEach(b=>b.remove());document.body.classList.remove('offcanvas-open');document.body.style.removeProperty('overflow');document.body.style.removeProperty('padding-right');authView()}
function boot(){S.user=S.token&&parse(S.token);S.user&&S.user.exp*1000>Date.now()?shell():(S.token&&sessionStorage.removeItem('ex_t'),S.token=null,authView())}

/* ---------- kerangka aplikasi ---------- */
function shell(){
 const adm=S.user.roles.some(r=>/^(Administrator|Auditor)$/.test(r));
 const nav=[['ringkasan','Ringkasan'],['kasus','Kasus'],['bukti','Barang bukti'],['serah','Serah terima'],...(adm?[['audit','Jejak audit']]:[])];
 const links=nav.map(([k,l])=>`<a href="#/${k}" data-k="${k}">${l}</a>`).join('');
 const dlinks=nav.map(([k,l])=>`<a href="#/${k}" data-k="${k}" data-bs-dismiss="offcanvas">${l}</a>`).join('');
 const me=`<div>${esc(S.user.email)}</div><div>${esc(S.user.roles.join(', '))}</div><button class="btn btn-outline-dark" data-act="copyid">Salin ID saya</button><button class="btn btn-outline-dark" data-act="out">Keluar</button>`;
 root.innerHTML=`<div class="app"><aside><div class="logo">Evidence<b>X</b></div><nav>${links}<i class="ind"></i></nav>
 <div class="me">${me}</div></aside>
 <header class="mtop"><div class="logo">Evidence<b>X</b></div><button class="burger" type="button" data-bs-toggle="offcanvas" data-bs-target="#mnav" aria-controls="mnav" aria-label="Buka menu navigasi"><span></span><span></span></button></header>
 <div class="offcanvas offcanvas-end mnav" tabindex="-1" id="mnav" aria-labelledby="mnavT"><div class="offcanvas-header"><div class="logo" id="mnavT">Evidence<b>X</b></div><button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Tutup menu"></button></div><div class="offcanvas-body"><nav>${dlinks}</nav><div class="me">${me}</div></div></div>
 <main id="v"></main></div>`;
 const mn=$('#mnav'),bg=$('.burger');
 if(mn&&bg){mn.addEventListener('show.bs.offcanvas',()=>bg.classList.add('is-open'));mn.addEventListener('hide.bs.offcanvas',()=>bg.classList.remove('is-open'))}
 route()}
const V={};
async function route(){
 if(!S.user||!$('#v'))return;const my=++rt;tapeOn();
 const [k,arg]=(location.hash.slice(2)||'ringkasan').split('/'),fn=V[k]||V.ringkasan,key=V[k]?k:'ringkasan';
 $$('nav a').forEach(a=>a.classList.toggle('on',a.dataset.k===key));
 const on=$('nav a.on'),ind=$('.ind');if(on&&ind){ind.style.setProperty('--y',on.offsetTop+'px');ind.style.setProperty('--h',on.offsetHeight+'px')}
 const v=$('#v');v.className='';v.innerHTML='<div class="pn">'+'<div class="sk"></div>'.repeat(5)+'</div>';
 try{const [h,bind]=await fn(arg);if(my!==rt)return;tapeOff();v.innerHTML=h;v.className='pg';bind&&bind(v)}
 catch(e){if(my!==rt)return;tapeOff();v.className='pg';v.innerHTML=`<div class="pn em"><h3>Data tidak dapat dimuat</h3><p>${esc(e.message)}</p></div>`}}

/* ---------- halaman ---------- */
const evTable=(it,cm)=>it.length?`<div class="pn"><table class="table table-hover align-middle mb-0"><thead><tr><th>Kode</th><th>Berkas</th>${cm?'<th>Kasus</th>':''}<th>Ukuran</th><th>Pemegang</th><th>Terdaftar</th></tr></thead><tbody>${it.map((e,i)=>`<tr style="--i:${i}" tabindex="0" data-ev="${e.id}"><td><span class="mk">${esc(e.evidenceCode)}</span></td><td>${esc(e.originalFileName)}</td>${cm?`<td>${esc(cm[e.caseId]||short(e.caseId))}</td>`:''}<td>${size(e.sizeBytes)}</td><td>${e.currentCustodianId===S.user.id?'<span class="ch ok">Saya</span>':`<code>${short(e.currentCustodianId)}</code>`}</td><td>${fmt(e.registeredAt)}</td></tr>`).join('')}</tbody></table></div>`:empty('Belum ada barang bukti','Daftarkan berkas pertama untuk memulai rantai kustodi.');

V.ringkasan=async()=>{
 const [c,e,p]=await Promise.allSettled([api('/cases'),api('/evidence?pageSize=5'),api('/transfers/pending')]);
 const g=x=>x.status==='fulfilled'?x.value:null,cs=g(c)||[],ev=g(e)||{items:[],total:0},pd=g(p)||[];
 return[`<header class="tw-flex tw-flex-wrap tw-items-end tw-justify-between tw-gap-4 tw-mb-6 ph"><div><h1>Ringkasan</h1><p>Posisi terkini kasus dan barang bukti Anda.</p></div><button class="btn btn-warning pri" data-act="upload">Daftarkan bukti</button></header>
 <div class="tw-grid tw-grid-cols-1 sm:tw-grid-cols-3 tw-gap-4 sts">${[['Kasus',cs.length],['Barang bukti',ev.total],['Serah terima menunggu Anda',pd.length]].map(([l,n])=>`<div class="tw-bg-[#F9FAF8] tw-border-2 tw-border-[#101820] tw-rounded-md tw-px-5 tw-py-4 st"><b data-n="${n}">0</b><span>${l}</span></div>`).join('')}</div>
 <h2 class="tw-text-3xl tw-mt-8 tw-mb-3 sh">Terakhir didaftarkan</h2>${evTable(ev.items)}`,el=>$$('[data-n]',el).forEach(countUp)]};

V.kasus=async()=>{const cs=await api('/cases');
 return[`<header class="tw-flex tw-flex-wrap tw-items-end tw-justify-between tw-gap-4 tw-mb-6 ph"><div><h1>Kasus</h1><p>${cs.length} kasus tercatat. Pilih kasus untuk melihat barang buktinya.</p></div><button class="btn btn-warning pri" data-act="newcase">Kasus baru</button></header>
 ${cs.length?`<div class="tw-grid tw-grid-cols-1 md:tw-grid-cols-2 xl:tw-grid-cols-3 tw-gap-4 cg">${cs.map((c,i)=>`<a class="cc" style="--i:${i}" href="#/bukti/${c.id}"><span class="mk">${esc(c.caseNumber)}</span><h3>${esc(c.title)}</h3><p>${esc(c.description||'Tanpa deskripsi.')}</p><small><span class="ch wait">${esc(c.status)}</span> dibuka ${fmt(c.createdAt)}</small></a>`).join('')}</div>`:empty('Belum ada kasus','Buat kasus pertama, lalu daftarkan barang bukti ke dalamnya.')}`]};

V.bukti=async cid=>{cid=cid||'';if(cid!==evCid){evCid=cid;evPage=1}
 const[cs,r]=await Promise.all([api('/cases'),api(`/evidence?page=${evPage}&pageSize=15${cid?'&caseId='+cid:''}`)]);
 const cm=Object.fromEntries(cs.map(c=>[c.id,c.caseNumber])),pages=Math.max(1,Math.ceil(r.total/r.pageSize));
 return[`<header class="tw-flex tw-flex-wrap tw-items-end tw-justify-between tw-gap-4 tw-mb-6 ph"><div><h1>Barang bukti</h1><p>${r.total} berkas${cid?' pada kasus ini':''}.</p></div><div class="tw-flex tw-flex-wrap tw-items-center tw-gap-2 acts"><select class="form-select w-auto" data-act="fc" aria-label="Filter kasus"><option value="">Semua kasus</option>${cs.map(c=>`<option value="${c.id}" ${c.id===cid?'selected':''}>${esc(c.caseNumber)} ${esc(c.title)}</option>`).join('')}</select><button class="btn btn-warning pri" data-act="upload">Daftarkan bukti</button></div></header>
 ${evTable(r.items,cm)}<div class="tw-flex tw-items-center tw-justify-end tw-gap-4 tw-mt-4"><button class="btn btn-outline-dark" data-act="pg" data-p="${evPage-1}" ${evPage<2?'disabled':''}>Sebelumnya</button><span>Halaman ${evPage} dari ${pages}</span><button class="btn btn-outline-dark" data-act="pg" data-p="${evPage+1}" ${evPage>=pages?'disabled':''}>Berikutnya</button></div>`]};

V.serah=async()=>{const p=await api('/transfers/pending');
 const ev=await Promise.all(p.map(t=>api('/evidence/'+t.evidenceId).catch(()=>null)));
 return[`<header class="tw-flex tw-flex-wrap tw-items-end tw-justify-between tw-gap-4 tw-mb-6 ph"><div><h1>Serah terima</h1><p>Permintaan kustodi yang menunggu keputusan Anda.</p></div></header>
 ${p.length?`<div class="tw-grid tw-grid-cols-1 md:tw-grid-cols-2 xl:tw-grid-cols-3 tw-gap-4 cg">${p.map((t,i)=>`<article class="cc" style="--i:${i}" data-t="${t.id}"><span class="mk">${esc(ev[i]?.evidenceCode||short(t.evidenceId))}</span><h3>${esc(ev[i]?.originalFileName||'Barang bukti')}</h3><p>${esc(t.purpose)}</p>${t.notes?`<p class="hint">${esc(t.notes)}</p>`:''}<small>Dari <code>${short(t.fromUserId)}</code>, ${fmt(t.requestedAt)}</small><div class="tw-flex tw-flex-wrap tw-items-center tw-gap-2 acts"><button class="btn btn-warning pri" data-act="acc">Terima</button><button class="btn btn-outline-danger" data-act="rej">Tolak</button></div></article>`).join('')}</div>`:empty('Tidak ada permintaan menunggu','Saat seseorang mengirim barang bukti kepada Anda, permintaannya muncul di sini.')}`]};

const evRow=(e,i)=>`<li class="ev" data-s="${e.sequenceNumber}" style="--i:${i%20}"><span class="mk">#${e.sequenceNumber}</span><div><b>${esc(e.eventType)}</b> <small>${esc(e.entityType)} ${short(e.entityId)}</small><div class="hint">${e.actorId?'oleh <code>'+short(e.actorId)+'</code>':'sistem'}, ${fmt(e.occurredAt)}</div></div><code>${esc(e.eventHash.slice(0,14))}</code></li>`;
V.audit=async()=>{const ev=await api('/audit?take=100');auLast=ev.at(-1)?.sequenceNumber||0;
 return[`<header class="tw-flex tw-flex-wrap tw-items-end tw-justify-between tw-gap-4 tw-mb-6 ph"><div><h1>Jejak audit</h1><p>Setiap peristiwa mengunci hash peristiwa sebelumnya.</p></div><button class="btn btn-warning pri" data-act="chain">Verifikasi rantai</button></header><div id="res"></div>
 ${ev.length?`<ol class="au">${ev.map(evRow).join('')}</ol>${ev.length===100?'<button class="btn btn-outline-dark" data-act="more">Muat lebih banyak</button>':''}`:empty('Belum ada peristiwa','Peristiwa tercatat otomatis saat bukti didaftarkan, diunduh, atau berpindah tangan.')}`]};

/* ---------- panel detail barang bukti ---------- */
const chk=c=>{const s=en(c.status,['Match','Mismatch','Error']);return`<li><span class="ch ${s==='Match'?'ok':'bad'}">${s==='Match'?'Cocok':s==='Mismatch'?'Tidak cocok':'Galat'}</span> <small>${fmt(c.checkedAt)}</small></li>`};
async function openEv(id){
 const d=dialog('Memuat','<div class="sk"></div><div class="sk"></div><div class="sk"></div>','drw');
 try{const[e,cu,ck]=await Promise.all([api('/evidence/'+id),api(`/evidence/${id}/custody`),api(`/evidence/${id}/integrity/checks`)]);
  d._e=e;$('h3',d).textContent=e.evidenceCode;const mine=e.currentCustodianId===S.user.id;
  $('.bd',d).innerHTML=`<dl class="kv"><dt>Berkas</dt><dd>${esc(e.originalFileName)}</dd><dt>Tipe dan ukuran</dt><dd>${esc(e.contentType)}, ${size(e.sizeBytes)}</dd><dt>Terdaftar</dt><dd>${fmt(e.registeredAt)}</dd><dt>Pemegang</dt><dd>${mine?'Anda':`<code>${esc(e.currentCustodianId)}</code>`}</dd><dt>SHA-256 asli</dt><dd><code>${esc(e.originalSha256)}</code></dd></dl>
  <div class="tw-flex tw-flex-wrap tw-items-center tw-gap-2 acts"><button class="btn btn-outline-dark" data-act="dl">Unduh</button>${mine?'<button class="btn btn-outline-dark" data-act="xfer">Ajukan serah terima</button>':''}<button class="btn btn-warning pri" data-act="vf">Verifikasi integritas</button></div>
  <div class="scan" hidden><div class="hl"><span>Hash terdaftar</span><code data-h1></code></div><div class="hl"><span>Hash berkas saat ini</span><code data-h2></code></div><div class="stamp"></div></div>
  <h4>Rantai kustodi</h4>${cu.length?`<ol class="tl">${cu.map((t,i)=>{const s=en(t.status,['Pending','Accepted','Rejected','Cancelled']);return`<li style="--i:${i}"><span class="ch ${s==='Accepted'?'ok':s==='Rejected'?'bad':'wait'}">${{Pending:'Menunggu',Accepted:'Diterima',Rejected:'Ditolak',Cancelled:'Dibatalkan'}[s]}</span> ${esc(t.purpose)}<br><small><code>${short(t.fromUserId)}</code> ke <code>${short(t.toUserId)}</code>, ${fmt(t.requestedAt)}</small></li>`}).join('')}</ol>`:'<p class="hint">Belum pernah berpindah tangan.</p>'}
  <h4>Riwayat verifikasi</h4><ul class="tl" id="hist">${ck.length?ck.map(chk).join(''):'<li class="hint">Belum ada verifikasi.</li>'}</ul>
  <h4>Arsip</h4><button class="btn btn-outline-danger" data-act="arc">Arsipkan</button>`}
 catch(x){d.close();toast(x.message,'bad')}}

/* ---------- unggah ---------- */
async function uploadDlg(){
 let cs;try{cs=await api('/cases')}catch(x){return toast(x.message,'bad')}
 if(!cs.length)return toast('Buat kasus lebih dulu sebelum mendaftarkan bukti.','bad');
 const d=dialog('Daftarkan barang bukti',`<form><label class="form-label w-100 mb-3">Kasus<select class="form-select" name="c">${cs.map(c=>`<option value="${c.id}" ${c.id===evCid?'selected':''}>${esc(c.caseNumber)} ${esc(c.title)}</option>`).join('')}</select></label>
 <div class="dz" tabindex="0"><b>Tarik berkas ke sini</b><br>atau klik untuk memilih<input type="file" hidden></div><p class="fn hint"></p><div class="progress my-3" hidden><div class="progress-bar progress-bar-striped progress-bar-animated bg-warning"></div></div><p class="err" role="alert"></p><button class="btn btn-warning pri" disabled>Daftarkan</button></form>
 <div class="done" hidden><p>Berkas terdaftar. Sidik jari SHA-256:</p><code data-h></code><button class="btn btn-outline-dark" data-bs-dismiss="modal">Selesai</button></div>`);
 const f=$('form',d),dz=$('.dz',d),inp=$('input[type=file]',d);let file;
 const pick=x=>{file=x;$('.fn',d).textContent=x?`${x.name} (${size(x.size)})`:'';$('.btn.pri',f).disabled=!x};
 dz.onclick=()=>inp.click();dz.onkeydown=e=>e.key==='Enter'&&inp.click();inp.onchange=()=>pick(inp.files[0]);
 ['dragenter','dragover'].forEach(n=>dz.addEventListener(n,e=>{e.preventDefault();dz.classList.add('hv')}));
 ['dragleave','drop'].forEach(n=>dz.addEventListener(n,e=>{e.preventDefault();dz.classList.remove('hv')}));
 dz.addEventListener('drop',e=>pick(e.dataTransfer.files[0]));
 f.onsubmit=e=>{e.preventDefault();const b=$('.btn.pri',f),tp=$('.progress',d),bar=$('.progress-bar',tp);b.disabled=true;tp.hidden=false;
  const fd=new FormData();fd.append('caseId',f.c.value);fd.append('file',file);
  const x=new XMLHttpRequest();x.open('POST','/api/evidence');x.setRequestHeader('Authorization','Bearer '+S.token);
  x.upload.onprogress=p=>bar.style.width=(p.loaded/p.total*100)+'%';
  x.onload=async()=>{if(x.status<300){const r=JSON.parse(x.responseText);f.hidden=true;$('.done',d).hidden=false;toast('Barang bukti terdaftar','ok');await scramble($('[data-h]',d),r.originalSha256,1200);route()}
   else{let m=x.responseText;try{m=JSON.parse(m).message||JSON.parse(m).title||m}catch{}$('.err',f).textContent=x.status===403?'Anda tidak memiliki izin untuk aksi ini.':m||'Unggah gagal.';tp.hidden=true;b.disabled=false}};
  x.onerror=()=>{$('.err',f).textContent='Server tidak dapat dihubungi.';tp.hidden=true;b.disabled=false};x.send(fd)}}

/* ---------- aksi ---------- */
const A={
 out:logout,upload:uploadDlg,
 copyid:async()=>{await navigator.clipboard.writeText(S.user.id);toast('ID pengguna disalin. Bagikan ke pengirim serah terima.','ok')},
 pg:b=>{evPage=+b.dataset.p;route()},
 newcase:()=>formDlg('Kasus baru',[['title','Judul kasus','text',1],['description','Deskripsi','area']],'Buat kasus',async(d,dl)=>{await api('/cases',{method:'POST',json:{title:d.title,description:d.description}});dl.close();toast('Kasus dibuat','ok');route()}),
 dl:async b=>{const e=b.closest('.ovr')._e,bl=await api(`/evidence/${e.id}/download`,{blob:1}),a=document.createElement('a');a.href=URL.createObjectURL(bl);a.download=e.originalFileName;a.click();setTimeout(()=>URL.revokeObjectURL(a.href),4000);toast('Unduhan dimulai','ok')},
 xfer:b=>{const e=b.closest('.ovr')._e,dr=b.closest('.ovr');formDlg('Ajukan serah terima',[['to','ID pengguna penerima','uuid',1],['purpose','Tujuan serah terima','text',1],['notes','Catatan','area']],'Kirim permintaan',async(d,dl)=>{await api(`/evidence/${e.id}/transfers`,{method:'POST',json:{toUserId:d.to.trim(),purpose:d.purpose,notes:d.notes||null}});dl.close();dr.close();toast('Permintaan serah terima terkirim','ok')})},
 arc:async b=>{if(!b.dataset.sure){b.dataset.sure=1;b.textContent='Klik lagi untuk mengarsipkan';setTimeout(()=>{delete b.dataset.sure;b.textContent='Arsipkan'},3500);return}
  const d=b.closest('.ovr');await api(`/evidence/${d._e.id}/archive`,{method:'POST'});d.close();toast('Barang bukti diarsipkan','ok');route()},
 vf:async b=>{const d=b.closest('.ovr'),e=d._e,sc=$('.scan',d),h1=$('[data-h1]',sc),h2=$('[data-h2]',sc),st=$('.stamp',sc);
  sc.hidden=false;sc.classList.remove('ok','bad');sc.classList.add('run');st.className='stamp';h1.textContent=e.originalSha256;b.disabled=true;let go=true,r,err;
  (async()=>{while(go){h2.textContent=rh(64);await sleep(60)}})();
  try{[r]=await Promise.all([api(`/evidence/${e.id}/integrity/verify`,{method:'POST'}),sleep(1500)])}catch(x){err=x}
  go=false;sc.classList.remove('run');b.disabled=false;
  if(err){h2.textContent='Tidak dapat diverifikasi';return toast(err.message,'bad')}
  const s=en(r.status,['Match','Mismatch','Error']);
  if(r.actualHash)await scramble(h2,r.actualHash,700);else h2.textContent='Berkas tidak dapat dibaca';
  st.textContent=s==='Match'?'Cocok':s==='Mismatch'?'Tidak cocok':'Galat';st.className='stamp '+(s==='Match'?'ok':'bad');sc.classList.add(s==='Match'?'ok':'bad');
  const h=$('#hist',d);$('.hint',h)?.remove();h.insertAdjacentHTML('afterbegin',chk(r))},
 acc:b=>decide(b,'accept','Serah terima diterima'),rej:b=>decide(b,'reject','Serah terima ditolak'),
 more:async b=>{const ev=await api('/audit?take=100&afterSequence='+auLast);auLast=ev.at(-1)?.sequenceNumber||auLast;
  $('.au').insertAdjacentHTML('beforeend',ev.map(evRow).join(''));if(ev.length<100)b.remove()},
 chain:async b=>{b.disabled=true;$('.au')?.classList.add('scanning');const res=$('#res');res.innerHTML='';const L=$$('.ev');L.forEach(l=>l.className='ev');
  try{const r=await api('/audit/verify-chain',{method:'POST'});
   const bi=r.valid?-1:L.findIndex(l=>+l.dataset.s===r.brokenAt),lim=bi<0?L.length:bi;
   for(let i=0;i<lim;i++){L[i].classList.add('ok');await sleep(reduce?0:Math.min(70,1800/L.length))}
   if(bi>=0)L[bi].classList.add('bad');
   res.innerHTML=r.valid?`<div class="bn ok"><b>Rantai utuh.</b> ${r.eventsChecked} peristiwa diperiksa. Hash kepala: <code>${esc(r.headHash)}</code></div>`:`<div class="bn bad"><b>Rantai putus pada #${r.brokenAt}.</b> ${esc(r.reason)}</div>`}
  finally{b.disabled=false;$('.au')?.classList.remove('scanning')}}
};
async function decide(b,act,msg){const c=b.closest('[data-t]');$$('.btn',c).forEach(x=>x.disabled=true);
 try{await api(`/transfers/${c.dataset.t}/${act}`,{method:'POST'});c.classList.add('gone');toast(msg,'ok');setTimeout(()=>{c.remove();$('.cg')&&!$('.cg').children.length&&route()},450)}
 catch(x){$$('.btn',c).forEach(y=>y.disabled=false);throw x}}

document.addEventListener('click',async e=>{
 const b=e.target.closest('[data-act]');
 if(b){const f=A[b.dataset.act];if(f)try{await f(b,e)}catch(x){toast(x.message,'bad');b.disabled=false}return}
 const r=e.target.closest('[data-ev]');if(r)openEv(r.dataset.ev)});
document.addEventListener('change',e=>{if(e.target.dataset.act==='fc')location.hash='#/bukti'+(e.target.value?'/'+e.target.value:'')});
document.addEventListener('keydown',e=>{
 if(e.key==='Enter'&&e.target.matches('tr[data-ev]'))openEv(e.target.dataset.ev)});
addEventListener('hashchange',route);
/* animasi tambahan */
const tb=document.createElement('div');tb.id='tb';document.body.append(tb);
function tapeOn(){tb.style.transition='none';tb.style.transform='scaleX(0)';tb.style.opacity=1;void tb.offsetWidth;tb.style.transition='transform 1.4s cubic-bezier(.2,.8,.2,1)';tb.style.transform='scaleX(.8)'}
function tapeOff(){tb.style.transition='transform .25s,opacity .4s .2s';tb.style.transform='scaleX(1)';tb.style.opacity=0}
document.addEventListener('pointerdown',e=>{const b=e.target.closest('.btn');if(!b||reduce||b.disabled)return;const r=b.getBoundingClientRect(),s=document.createElement('span');s.className='rip';s.style.cssText=`left:${e.clientX-r.left-60}px;top:${e.clientY-r.top-60}px`;b.append(s);setTimeout(()=>s.remove(),600)});
document.addEventListener('pointermove',e=>{const c=e.target.closest('a.cc');if(!c||reduce)return;const r=c.getBoundingClientRect();c.style.setProperty('--rx',((e.clientX-r.left)/r.width-.5)*8+'deg');c.style.setProperty('--ry',((e.clientY-r.top)/r.height-.5)*-8+'deg')});
document.addEventListener('pointerout',e=>{const c=e.target.closest('a.cc');if(c&&!c.contains(e.relatedTarget)){c.style.setProperty('--rx','0deg');c.style.setProperty('--ry','0deg')}});
boot();
})();
