import React, {useState, useEffect, useContext} from 'react'

// Components
import {AddDropdown} from '../../reusables/Dropdown/AddDropdown'
import {Input} from '../../reusables/Input/Input'
import 'react-datepicker/dist/react-datepicker.css'

// Utils
import {concatStyles as s} from '../../../utilities/mikesConcat'

// Styles
import styles from './DepartmentDetailEditPage.module.css'
import {Button} from '../../reusables/Button/Button'
import {AxiosService} from '../../../services/AxiosService/AxiosService'
import {LoginContext, ThemeContext} from '../../App/App'
import {format} from '../../../utilities/formatEmptyStrings'
import {cloneDeep} from 'lodash'
import {DetailPageTable} from '../../reusables/DetailPageTable/DetailPageTable'
import {PictureInput} from '../../reusables/PictureInput/PictureInput'
import {BackButton} from '../../reusables/BackButton/BackButton'
import {putUploadImage} from '../../../utilities/UploadImage'

// Types
interface IDepartmentDetailEditPageProps {
    match: any
    history: any
}
interface Defaults {
    value: string
    sortBy: string
    id: number
}

// Primary Component
export const DepartmentDetailEditPage: React.SFC<IDepartmentDetailEditPageProps> = props => {
    const {history, match} = props

    const [deptList] = useState<any>([])
    var deptsRowOne: any[] = []
    var deptsRowTwo: any[] = []
    //push them into alternating rows so that rows are equal
    for (let i = 0; i < deptList.length; i++) {
        if (i % 2 === 0) {
            deptsRowOne.push(deptList[i].DepartmentName)
        } else {
            deptsRowTwo.push(deptList[i].DepartmentName)
        }
    }

    const {loginContextVariables} = useContext(LoginContext)
    const {isDarkMode} = useContext(ThemeContext)

    const axios = new AxiosService(loginContextVariables)
    const [deptData, setDeptData] = useState<any>({})
    const [hardwareRows, setHardwareRows] = useState<any[]>([])
    const [softwareRows, setSoftwareRows] = useState<any[]>([])
    const [licenseRows, setLicenseRows] = useState<any[]>([])

    const hardwareHeaders = ['Hardware']
    const softwareHeaders = ['Software']
    const licenseHeaders = ['Licenses']

    //all possible hardware, software, and licenses
    const [hardwareDropdown, setHardwareDropdown] = useState<{name: string; id: number}[]>([])
    const [softwareDropdown, setSoftwareDropdown] = useState<{name: string; id: number}[]>([])
    const [licenseDropdown, setLicenseDropdown] = useState<{name: string; id: number}[]>([])

    const [imgInput, setImgInput] = useState<File>()

    useEffect(() => {
        axios
            .get(`/detail/department/${match.params.id}`)
            .then((data: any) => {
                let dept: any = {
                    // photo: data[0].picture,'
                    departmentName: data[0].departmentName,
                }
                setDeptData(dept)

                let hw: any[] = []
                data[0].defaultHardware.map((item: string, index: number) =>
                    hw.push([{value: format(item), sortBy: item, id: index}])
                )
                setHardwareRows(hw)

                let sw: any[] = []
                data[0].defaultSoftware.map((item: string, index: number) =>
                    sw.push([{value: format(item), sortBy: item, id: index}])
                )
                setSoftwareRows(sw)

                let l: any[] = []
                data[0].defaultLicenses.map((item: string, index: number) =>
                    l.push([{value: format(item), sortBy: item, id: index}])
                )
                setLicenseRows(l)
            })
            .catch((err: any) => console.error(err))
    }, [])

    //get the lists of all possible hardware, licenses, and software
    useEffect(() => {
        axios.get(`/add/departmentprep`).then((data: any) => {
            let hw: {name: string; id: number}[] = []
            data[0].hardware.map(
                (hardware: string, index: number) => hw.push({name: hardware, id: index}) //gives each hardware unique id
            )
            setHardwareDropdown(hw)
            let sw: {name: string; id: number}[] = []
            data[0].software.map(
                (software: string, index: number) => sw.push({name: software, id: index}) //gives each hardware unique id
            )
            setSoftwareDropdown(sw)

            let l: {name: string; id: number}[] = []
            data[0].licenses.map(
                (licenses: string, index: number) => l.push({name: licenses, id: index}) //gives each hardware unique id
            )
            setLicenseDropdown(l)
        })
    }, [])

    const handleAddHardware = (name: string) => {
        let tempArray = cloneDeep(hardwareRows)
        tempArray.push([{value: format(name), sortBy: name, id: tempArray.length}])
        setHardwareRows(tempArray)
    }
    const handleRemoveHardware = (name: Defaults[]) => {
        let tempArray = cloneDeep(hardwareRows)
        tempArray = tempArray.filter(item => item[0].id !== name[0].id)
        setHardwareRows(tempArray)
    }

    const handleAddSoftware = (name: string) => {
        let tempArray = cloneDeep(softwareRows)
        tempArray.push([{value: format(name), sortBy: name, id: tempArray.length}])
        setSoftwareRows(tempArray)
    }
    const handleRemoveSoftware = (name: Defaults[]) => {
        let tempArray = cloneDeep(softwareRows)
        tempArray = tempArray.filter(item => item[0].id !== name[0].id)
        setSoftwareRows(tempArray)
    }

    const handleAddLicense = (name: string) => {
        let tempArray = cloneDeep(licenseRows)
        tempArray.push([{value: format(name), sortBy: name, id: tempArray.length}])
        setLicenseRows(tempArray)
    }
    const handleRemoveLicense = (name: Defaults[]) => {
        let tempArray = cloneDeep(licenseRows)
        tempArray = tempArray.filter(item => item[0].id !== name[0].id)
        setLicenseRows(tempArray)
    }

    //takes in the array of array of objects (type:Defaults) and returns
    //an array of strings to send to the database
    function formatForPost(rows: Defaults[][]) {
        let tempArray: string[] = []
        for (let i = 0; i < rows.length; i++) {
            tempArray.push(rows[i][0].value)
        }
        return tempArray
    }
    async function handleSubmit() {
        var newID = ''
        var imgID
        if (
            //check to make sure there is a proper entry in department name
            deptData.departmentName === '' ||
            deptData.departmentName === null ||
            deptData.departmentName === undefined
        ) {
            window.alert('Department must have a name!')
        } else {
            let newDefaultHardware = formatForPost(hardwareRows)
            let newDefaultSoftware = formatForPost(softwareRows)
            let newDefaultLicenses = formatForPost(licenseRows)
            if (match.params.id === 'new') {
                await axios
                    .post(`add/department`, {
                        DefaultHardware: {DefaultHardware: newDefaultHardware},
                        DefaultPrograms: {
                            license: newDefaultLicenses,
                            software: newDefaultSoftware,
                        },
                        name: deptData.departmentName,
                    })
                    .then((response: any) => {
                        newID = imgID = response.data
                    })
                history.push({pathname: `/departments/detail/${newID}`, state: {prev: history.location}})
            } else {
                await axios.put(`update/department`, {
                    DefaultHardware: {DefaultHardware: newDefaultHardware},
                    DefaultPrograms: {
                        license: newDefaultLicenses,
                        software: newDefaultSoftware,
                    },
                    name: deptData.departmentName,
                    ID: match.params.id,
                })
                history.push({pathname: `/departments/detail/${match.params.id}`, state: {prev: history.location}})
                imgID = match.params.id
            }
        }
        // Update Image.
        if (imgInput) {
            putUploadImage(imgInput, `/image/department/${imgID}`, axios)
        }
    }

    return (
        <div className={s(styles.columns, isDarkMode ? styles.backgroundDark : {})}>
            {/* column 1 */}

            <div className={styles.firstColumn}>
                <BackButton history={history} className={styles.backButton} />

                <div className={styles.imgContainer}>
                    <PictureInput setImage={setImgInput} image={imgInput} />
                </div>
                <div className={styles.submitContainer}>
                    <Button text='Submit' onClick={handleSubmit} />
                </div>
            </div>
            {/* column 2 */}
            <div className={styles.secondColumn}>
                {/* name and date */}
                <div className={s(styles.title, styles.paddingTop, isDarkMode ? styles.textDark : {})}>
                    Department Information
                </div>

                <div className={styles.row}>
                    <Input
                        label={'Department Name'}
                        value={deptData.departmentName}
                        onChange={(e: any) => setDeptData({...deptData, departmentName: e.target.value})}
                        maxChars={50}
                        className={styles.deptNameInput}
                    />
                </div>

                {/* Tables */}
                <div className={styles.tableColumn}>
                    <div className={styles.firstTableColumn}>
                        {/* default hardware */}
                        <div className={styles.table}>
                            <DetailPageTable
                                headers={hardwareHeaders}
                                rows={hardwareRows}
                                setRows={setHardwareRows}
                                style={styles.newRowThing}
                                edit={true}
                                remove={handleRemoveHardware}
                            />

                            {hardwareDropdown && (
                                <AddDropdown
                                    title={'Add default hardware'}
                                    content={hardwareDropdown}
                                    onSelect={(i: {name: string; id: string | number}) => handleAddHardware(i.name)}
                                    className={s(styles.addDefaultContainer, styles.zIndex3)}
                                />
                            )}
                        </div>
                        {/* default licenses */}
                        <div className={styles.table}>
                            <DetailPageTable
                                headers={licenseHeaders}
                                rows={licenseRows}
                                setRows={setLicenseRows}
                                style={styles.newRowThing}
                                edit={true}
                                remove={handleRemoveLicense}
                            />

                            {licenseDropdown && (
                                <AddDropdown
                                    title={'Add default license'}
                                    content={licenseDropdown}
                                    onSelect={(i: {name: string; id: string | number}) => handleAddLicense(i.name)}
                                    className={styles.addDefaultContainer}
                                />
                            )}
                        </div>
                    </div>
                    {/* default software */}
                    <div className={styles.secondTableColumn}>
                        <div className={styles.table}>
                            <DetailPageTable
                                headers={softwareHeaders}
                                rows={softwareRows}
                                setRows={setSoftwareRows}
                                style={styles.newRowThing}
                                edit={true}
                                remove={handleRemoveSoftware}
                            />
                            {softwareDropdown && (
                                <AddDropdown
                                    title={'Add default software'}
                                    content={softwareDropdown}
                                    onSelect={(i: {name: string; id: string | number}) => handleAddSoftware(i.name)}
                                    className={styles.addDefaultContainer}
                                />
                            )}
                        </div>
                    </div>
                </div>
                <div className={styles.submitContainer}>
                    <Button text='Submit' onClick={handleSubmit} />
                </div>
            </div>
        </div>
    )
}
